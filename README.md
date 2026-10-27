# EventHub

REST API для управления событиями (events) и бронированиями (bookings) на ASP.NET Core 10 (minimal API) с хранением данных в PostgreSQL.

## Стек

- ASP.NET Core 10, minimal API (`EventHub.Api/Endpoints/EventEndpoints.cs`, `BookingEndpoints.cs`) — контроллеров в проекте нет
- Слоистая архитектура из четырёх проектов: `Domain` ← `Application` ← `Infrastructure` ← `Api` (см. [Архитектура](#архитектура))
- PostgreSQL + EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`), доступ к данным через репозитории (`EventHub.Infrastructure/Repositories`)
- Миграции EF Core лежат в `EventHub.Infrastructure/Migrations`, применяются автоматически при старте (`db.Database.Migrate()` в `Program.cs`)
- `BackgroundService` — асинхронная обработка бронирований
- Ошибки — доменные исключения (`EventHub.Domain/Exceptions`) превращаются в `ProblemDetails` (RFC 9457) через `IExceptionHandler` в `EventHub.Api` (`Common/Middlewares/GlobalExceptionHandlingMiddleware.cs`)
- Swagger UI (Swashbuckle) поверх встроенной OpenAPI-спеки `Microsoft.AspNetCore.OpenApi`
- Docker Compose: Postgres + API одной командой

## Архитектура

Решение разбито на четыре проекта, зависимости направлены внутрь:

```
EventHub.Api ──► EventHub.Infrastructure ──► EventHab.Application ──► EventHub.Domain
     └──────────────────────────────────────────►┘
```

| Проект | Что внутри | Зависит от |
|--------|------------|------------|
| `EventHub.Domain` | Модели `Event`, `Booking`, `BookingStatus` с инвариантами; доменные исключения `NotFoundException`, `ValidationException`, `NoAvailableSeatsException` | — (чистый, без пакетов) |
| `EventHab.Application` | Сервисы `EventService`, `BookingService` и их интерфейсы; интерфейсы репозиториев `IEventRepository`, `IBookingRepository`; DTO (`Contracts`) | `Domain` |
| `EventHub.Infrastructure` | `AppDbContext`, конфигурации сущностей, миграции, реализации репозиториев (EF Core + Npgsql) | `Application`, `Domain` |
| `EventHub.Api` | Composition root (`Program.cs`), minimal API эндпоинты, `GlobalExceptionHandler`, `BookingProcessingBackgroundService` | `Application`, `Infrastructure` |

Ключевые решения:

- **Интерфейс репозитория объявлен в `Application`, реализация — в `Infrastructure`.** Сервисы не знают про EF Core: `Application` не ссылается на пакет `Microsoft.EntityFrameworkCore`, поэтому репозиторий возвращает уже материализованный `IReadOnlyList<Event>`, а `Skip`/`Take`/`ToListAsync` выполняются внутри `EventRepository`.
- **Доменные исключения не знают про HTTP.** В `Domain` нет `ProblemDetails` и статус-кодов; соответствие «исключение → HTTP-статус» живёт только в `EventHub.Api/Common/Middlewares/GlobalExceptionHandlingMiddleware.cs`.
- **Связывание через DI — только в `Program.cs`** (`AddScoped<IEventRepository, EventRepository>()` и т.д.).

## Быстрый старт через Docker

Из каталога `EventHub.Api/` (там лежит `compose.yaml` и решение `EventHub.sln`):

```bash
docker compose up -d --build     # поднять Postgres + API
docker compose logs -f eventhub.api
docker compose down              # остановить (с удалением данных: docker compose down -v)
```

| Что | Адрес |
|-----|-------|
| Swagger UI | http://localhost:8080/swagger (корень `http://localhost:8080/` редиректит сюда) |
| OpenAPI-спека | http://localhost:8080/openapi/v1.json |
| PostgreSQL | `localhost:5432`, БД `event`, пользователь/пароль `postgres`/`postgres` |

Важные детали compose-конфигурации:

- API стартует только после `healthcheck` Postgres (`depends_on: condition: service_healthy`) — иначе автоприменение миграций на старте падает;
- строка подключения передаётся переменной окружения `ConnectionStrings__DefaultConnection` с хостом `postgres` (имя сервиса в compose-сети, не `localhost`);
- внутри контейнера Kestrel слушает только HTTP (`ASPNETCORE_HTTP_PORTS=8080`), HTTPS-порт наружу не проброшен — по `https://localhost:8080` API не откроется;
- данные Postgres сохраняются в именованном томе `event_pgdata`.

## Локальный запуск без Docker

Нужен доступный PostgreSQL. По умолчанию используется строка подключения из `EventHub.Api/appsettings.json`:

```
ConnectionStrings:DefaultConnection = Host=localhost;Port=5432;Database=event;Username=postgres;Password=postgres
```

Поднять только базу можно тем же compose-файлом:

```bash
cd EventHub.Api
docker compose up -d postgres
dotnet run --project EventHub.Api
```

Порты для `dotnet run` берутся из `EventHub.Api/Properties/launchSettings.json` (`http://localhost:5092`, `https://localhost:7100`).

Имя ключа конфигурации важно: `Program.cs` читает именно `GetConnectionString("DefaultConnection")` и при отсутствующей строке падает на старте с явным сообщением, а не глубоко внутри Npgsql.

Вместе с API стартует фоновый сервис `BookingProcessingBackgroundService` (см. [Фоновая обработка бронирований](#фоновая-обработка-бронирований)) — отдельно запускать его не нужно, он регистрируется через `AddHostedService`.

## База данных и миграции

Схема описана через `IEntityTypeConfiguration` (`EventHub.Infrastructure/Configurations`), имена таблиц и колонок — snake_case с префиксом `cd_`:

| Сущность | Таблица | Колонки |
|----------|---------|---------|
| `Event` | `cd_events` | `id`, `title` (≤100), `description` (≤200), `start_at`, `end_at`, `total_seats`, `available_seats` |
| `Booking` | `cd_bookings` | `id`, `event_id` (FK → `cd_events`, `ON DELETE CASCADE`), `status` (строка, ≤20), `created_at`, `processed_at` |

`Id` обеих сущностей генерируется в домене (`Guid.NewGuid()`), а не базой (`ValueGeneratedNever`). `BookingStatus` хранится строкой (`HasConversion<string>`).

Миграции применяются автоматически на старте приложения. `AppDbContext` и миграции живут в `EventHub.Infrastructure`, а точкой запуска (со строкой подключения) служит `EventHub.Api`, поэтому нужны оба флага. Добавить новую:

```bash
cd EventHub.Api
dotnet ef migrations add <Name> --project EventHub.Infrastructure --startup-project EventHub.Api
```

## Тесты

Решение лежит не в корне репозитория, поэтому путь обязателен:

```bash
cd EventHub.Api
dotnet test EventHub.sln
```

- `EventHub.Tests` — юнит-тесты сервисов и доменных моделей;
- `EventHub.IntegrationTests` — тесты репозиториев (`EventRepositoryTests`, `BookingRepositoryTests`) и миграций (`MigrationTests`) на реальном PostgreSQL, который поднимается через [Testcontainers](https://dotnet.testcontainers.org/) (образ `postgres:16-alpine`). Нужен только запущенный Docker — отдельный `docker compose up` для них не требуется, контейнеры создаются и удаляются самими тестами.

`EventRepositoryTests` и `BookingRepositoryTests` делят один Postgres-контейнер через `PostgresFixture` (`ICollectionFixture`, коллекция `RepositoryCollection`) — схема строится через `EnsureCreated()`/`EnsureDeletedAsync()` из текущей модели, история миграций не проверяется; тесты внутри коллекции выполняются последовательно. `MigrationTests` — отдельный класс со своим изолированным контейнером (вне общей коллекции): гоняет `Database.MigrateAsync()` и проверяет через `GetAppliedMigrationsAsync()`, что `InitialCreate` реально применяется.

Запуск по отдельности:

```bash
dotnet test EventHub.Tests
dotnet test EventHub.IntegrationTests
dotnet test EventHub.sln --filter "FullyQualifiedName~EventRepositoryTests"
```

Сборка штатно выдаёт десятки `warning CS1591` (missing XML comment) — это существующий фон, а не следствие правок; при проверке результата удобно фильтровать вывод: `dotnet test EventHub.sln 2>&1 | grep -E "error|Passed!|Failed"`.

## Модель данных

### Event

| Поле             | Тип        | Обязательность          | Описание |
|------------------|------------|--------------------------|----------|
| `Id`             | `Guid`     | генерируется сервером    | Идентификатор события |
| `Title`          | `string`   | обязательно, ≤100 символов | Название события |
| `Description`    | `string?`  | опционально, ≤200 символов | Описание события |
| `StartAt`        | `DateTime` | обязательно              | Дата и время начала |
| `EndAt`          | `DateTime` | обязательно, позже `StartAt` | Дата и время окончания |
| `TotalSeats`     | `int`      | обязательно, `> 0`       | Общее количество мест |
| `AvailableSeats` | `int`      | генерируется сервером    | Свободные места: при создании равно `TotalSeats`, уменьшается в `Event.TryReserveSeats()`, возвращается в `Event.ReleaseSeats()`, пересчитывается в `Event.Update()` при изменении `TotalSeats` |

### Валидация события

Инварианты живут в самом домене — `Event.Create()` и `Event.Update()` вызывают общий `ThrowIfNotValid()`, поэтому проверки работают независимо от того, пришёл вызов из HTTP-DTO или нет:

- `Title` не пустой;
- `StartAt` и `EndAt` заданы;
- `StartAt` не в прошлом (`>= DateTime.UtcNow`);
- `EndAt` позже `StartAt`;
- `TotalSeats` больше `0`.

Нарушение бросает `EventHub.Domain.Exceptions.ValidationException` (собирает все ошибки по полям) → ответ `400 Bad Request`.

При изменении `TotalSeats` через `Event.Update()` пересчитывается `AvailableSeats = TotalSeats - забронированные места` (забронированные = `TotalSeats - AvailableSeats` до обновления). Если новое `TotalSeats` меньше уже забронированных мест — `Event.Update()` бросает `ValidationException`, а не молча уводит `AvailableSeats` в отрицательные значения.

### Booking

| Поле          | Тип             | Обязательность           | Описание |
|---------------|-----------------|---------------------------|----------|
| `Id`          | `Guid`          | генерируется сервером     | Идентификатор брони |
| `EventId`     | `Guid`          | обязательно               | Событие, к которому относится бронь |
| `Status`      | `BookingStatus` | генерируется сервером     | Текущий статус |
| `CreatedAt`   | `DateTime`      | генерируется сервером     | Момент создания |
| `ProcessedAt` | `DateTime?`     | заполняется при обработке | Момент смены статуса |

`BookingStatus`: `Pending` → `Confirmed` / `Rejected`. Статус меняется только доменными методами `Booking.Confirm()` / `Booking.Reject()`, которые атомарно проставляют `Status` и `ProcessedAt`.

Бронь создаётся только для существующего события: `BookingService.CreateBookingAsync` читает событие через `IEventRepository` и бросает `NotFoundException` (`404`), если его нет. Перед созданием брони резервируется место через `Event.TryReserveSeats()`; если свободных мест нет — `NoAvailableSeatsException` (`409 Conflict`), бронь не создаётся.

Проверка события и резервирование места выполняются под общим статическим `SemaphoreSlim` (`BookingService.BookingLock`), поэтому в рамках одного процесса овербукинг при конкурентных запросах невозможен. Это *внутрипроцессная* блокировка: при запуске нескольких экземпляров API она не защищает — потребуется блокировка на уровне БД.

## Эндпоинты

Все маршруты живут под префиксом `/api`.

### События — `/api/events`

| Метод  | Путь                       | Описание                        | Успех            | Ошибка |
|--------|----------------------------|----------------------------------|------------------|--------|
| GET    | `/api/events`              | Список событий с фильтрацией     | `200 OK`         | — |
| GET    | `/api/events/{id}`         | Событие по `id`                  | `200 OK`         | `404 Not Found` |
| POST   | `/api/events`              | Создать событие                  | `201 Created` + `Location` | `400 Bad Request` |
| PUT    | `/api/events/{id}`         | Обновить событие целиком         | `200 OK`         | `404 Not Found` / `400 Bad Request` |
| DELETE | `/api/events/{id}`         | Удалить событие                  | `204 No Content` | — |
| POST   | `/api/events/{id}/book`    | Создать бронь для события        | `202 Accepted` + `Location` | `404 Not Found` / `409 Conflict` |

### Бронирования — `/api/bookings`

| Метод | Путь                  | Описание         | Успех    | Ошибка |
|-------|-----------------------|-------------------|----------|--------|
| GET   | `/api/bookings/{id}`  | Бронь по `id`     | `200 OK` | `404 Not Found` |

`POST /api/events/{id}/book` отвечает `202 Accepted`, а не `201 Created`: бронь создаётся синхронно, но подтверждается/отклоняется асинхронно фоновым сервисом, поэтому на момент ответа она ещё в статусе `Pending`. В ответе — заголовок `Location: /api/bookings/{bookingId}`, по которому можно отследить итоговый статус.

Текущие особенности поведения, о которых стоит знать:

- `GET /api/events` принимает только фильтры `title`/`from`/`to`; параметры `page`/`pageSize` из query-строки **не читаются** — выдача всегда первая страница по 10 элементов (значения по умолчанию `EventService.GetAllEventsAsync`);
- фильтр `to` сравнивается с `StartAt` события (`StartAt <= to`), а не с `EndAt`;
- `DELETE /api/events/{id}` возвращает `204 No Content` и для несуществующего `id` — результат `DeleteEventAsync` не проверяется на уровне эндпоинта;
- `400 Bad Request` от `ValidationException` реально приходит без `errors` по полям: `GlobalExceptionHandler` объявляет переменную под `ValidationProblemDetails`/`ProblemDetails` как `ProblemDetails` (`EventHub.Api/Common/Middlewares/GlobalExceptionHandlingMiddleware.cs`), поэтому `WriteAsJsonAsync` сериализует по статическому типу и теряет словарь `errors` — в теле остаётся только `title`/`status`/`detail`.

### Примеры запросов

**Список событий с фильтрацией**

```http
GET /api/events?title=митинг&from=2026-07-01T00:00:00Z&to=2026-07-31T23:59:59Z
```

| Параметр | Тип         | Описание |
|----------|-------------|----------|
| `title`  | `string?`   | Частичное, регистронезависимое совпадение по названию |
| `from`   | `DateTime?` | Событие начинается не раньше указанной даты (`StartAt >= from`) |
| `to`     | `DateTime?` | Событие начинается не позже указанной даты (`StartAt <= to`) |

Ответ:

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Митинг команды",
      "description": "Еженедельный синк",
      "startAt": "2026-07-20T10:00:00Z",
      "endAt": "2026-07-20T11:00:00Z",
      "totalSeats": 10,
      "availableSeats": 9
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

**Создать событие**

```http
POST /api/events
Content-Type: application/json

{
  "title": "Митинг команды",
  "description": "Еженедельный синк",
  "startAt": "2026-07-20T10:00:00Z",
  "endAt": "2026-07-20T11:00:00Z",
  "totalSeats": 10
}
```

Ответ `201 Created`, заголовок `Location: /api/events/{id}`, тело — объект события (`EventInfo`).

**Обновить событие**

```http
PUT /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "title": "Митинг команды (перенесён)",
  "description": "Еженедельный синк",
  "startAt": "2026-07-21T10:00:00Z",
  "endAt": "2026-07-21T11:00:00Z",
  "totalSeats": 12
}
```

**Удалить событие**

```http
DELETE /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Создать бронь**

```http
POST /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
```

Ответ `202 Accepted` с заголовком `Location: /api/bookings/{bookingId}` и телом:

```json
{
  "id": "b1f2c3d4-0000-0000-0000-000000000000",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "createdAt": "2026-09-24T10:00:00Z",
  "processedAt": null
}
```

**Получить бронь**

```http
GET /api/bookings/b1f2c3d4-0000-0000-0000-000000000000
```

Через ~2 секунды после создания статус станет `Confirmed` (или `Rejected`, если событие успели удалить), а `processedAt` заполнится.

## Фоновая обработка бронирований

`BookingProcessingBackgroundService` (`EventHub.Api/Services/BookingProcessingBackgroundService.cs`) регистрируется через `AddHostedService` и работает всё время жизни приложения:

1. каждые 5 секунд (`PollingInterval`) в отдельном DI-scope запрашивает у `IBookingRepository.GetPendingIds()` идентификаторы броней в статусе `Pending`;
2. обрабатывает их **параллельно** через `Task.WhenAll`, по задаче `ProcessBookingAsync` на бронь.

Обработка одной брони:

1. `Task.Delay` на 2 секунды (`ProcessingDelay`) — имитация обращения к внешней системе; задержка идёт до работы с БД, поэтому у броней одного тика она параллельна, а не суммируется;
2. создаётся собственный DI-scope со своими `IBookingRepository`/`IEventRepository` — `DbContext` зарегистрирован как scoped и не может разделяться между параллельными задачами;
3. бронь перечитывается и пропускается, если её уже нет или статус не `Pending` (защита от повторной обработки);
4. если событие не найдено (удалено, пока бронь ждала) — `booking.Reject()` и `Warning` в лог; иначе `booking.Confirm()`;
5. при непредвиденном исключении бронь отклоняется, место возвращается в пул (`Event.ReleaseSeats()`), ошибка логируется; отмена по `CancellationToken` при остановке хоста обрабатывается отдельно и молча.

Поскольку каждая задача работает в своём scope и пишет в БД, отдельного общего примитива синхронизации фоновому сервису не требуется: блокировка в нём всё равно не видна HTTP-потокам и другим экземплярам приложения.

## Формат ответа и обработка ошибок

Успешные ответы — это DTO напрямую (`EventInfo`, `BookingInfo`, `PaginatedResult<EventInfo>`), без общего конверта.

Ошибки обрабатываются централизованно в `GlobalExceptionHandler` (`EventHub.Api/Common/Middlewares/GlobalExceptionHandlingMiddleware.cs`, реализует `IExceptionHandler`, подключён через `AddExceptionHandler` + `UseExceptionHandler`) и возвращаются в формате `ProblemDetails`. Сами исключения объявлены в `EventHub.Domain/Exceptions` и ничего не знают о HTTP — статус-код подбирается только в обработчике:

```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Event not found"
}
```

| Исключение                  | HTTP-статус | Когда бросается |
|-----------------------------|-------------|------------------|
| `NotFoundException`         | `404`       | Событие или бронь не найдены |
| `ValidationException`       | `400`       | Нарушены доменные инварианты события |
| `NoAvailableSeatsException` | `409`       | Нет свободных мест при создании брони |
| любое другое                | `500`       | Непредвиденная ошибка; текст исключения уходит только в лог |

Файлы конверта `EventHab.Application/Contracts/ApiResult.cs`, `EventHub.Api/Common/ApiResultActionResult.cs`, `ApiResultWithLocationResult.cs` и `ApiBaseResultExtensions.cs` остались от прежней версии на контроллерах и сейчас нигде не используются — minimal API-эндпоинты их не вызывают.

`EventHub.Domain.Exceptions.ValidationException` — собственный тип, затеняющий `System.ComponentModel.DataAnnotations.ValidationException`; когда оба в области видимости, нужен явный `using`-алиас (пример — `EventHub.Domain/Models/Event.cs`).

## Структура проекта

```
EventHub.Api/                       # каталог решения (EventHub.sln, compose.yaml)
├── compose.yaml                    # Postgres + API
├── EventHub.Domain/                # домен, без зависимостей
│   ├── Models/                     # Event, Booking, BookingStatus
│   └── Exceptions/                 # NotFoundException, ValidationException,
│                                   # NoAvailableSeatsException
├── EventHab.Application/           # сценарии использования
│   ├── Contracts/                  # EventInfo, CreateEvent, EventUpsert, EventFilter,
│   │                               # BookingInfo, PaginatedResult<T>, ApiResult (не используется)
│   ├── Repositories/               # IEventRepository, IBookingRepository
│   └── Services/
│       ├── Abstractions/           # IEventService, IBookingService
│       ├── EventService.cs
│       └── BookingService.cs
├── EventHub.Infrastructure/        # EF Core + PostgreSQL
│   ├── AppDbContext.cs
│   ├── Configurations/             # EventConfiguration, BookingConfiguration
│   ├── Migrations/
│   └── Repositories/               # EventRepository, BookingRepository
├── EventHub.Api/                   # composition root и HTTP-слой
│   ├── Dockerfile
│   ├── Endpoints/
│   │   ├── EventEndpoints.cs       # группа /api/events
│   │   └── BookingEndpoints.cs     # группа /api: /events/{id}/book, /bookings/{id}
│   ├── Services/
│   │   └── BookingProcessingBackgroundService.cs
│   ├── Common/
│   │   ├── Middlewares/GlobalExceptionHandlingMiddleware.cs  # класс GlobalExceptionHandler
│   │   └── ApiResult*.cs, ApiBaseResultExtensions.cs         # не используются
│   └── Program.cs
├── EventHub.Tests/                 # юнит-тесты
└── EventHub.IntegrationTests/      # тесты репозиториев на Testcontainers
```
