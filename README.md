# EventHub

# EventHub.Api

Простой REST API для управления событиями (events) и бронированиями (bookings) на ASP.NET Core.

## Стек

- ASP.NET Core Web API
- In-memory хранилище (статические коллекции, без БД)
- DataAnnotations + `IValidatableObject` для валидации DTO
- `BackgroundService` — фоновая обработка бронирований
- Единый JSON-конверт ответа (`ApiResult`) и централизованная обработка ошибок через middleware

## Запуск

```bash
dotnet restore
dotnet run --project EventHub.Api/EventHub.Api
```

По умолчанию API поднимется на `https://localhost:xxxx` (порт см. в `launchSettings.json` или в выводе консоли при старте). Swagger UI доступен по адресу `/swagger`, если подключён в проекте.

Вместе с API стартует фоновый сервис `BookingProcessingBackgroundService` (см. [Фоновая обработка бронирований](#фоновая-обработка-бронирований)) — отдельно запускать не нужно, он регистрируется через `AddHostedService` и работает в процессе приложения.

## Тесты

```bash
dotnet test EventHub.Api/EventHub.Api.sln
```

Прогоняет оба тестовых проекта разом:

- `EventHub.Tests` — юнит-тесты `EventService` (CRUD, фильтрация, пагинация, валидация DTO) и `BookingService` (создание брони, уникальность Id, смена статуса, обработка отсутствующего/удалённого события);
- `EventHub.IntegrationTests` — HTTP-тесты через `WebApplicationFactory<Program>` (реальные статусы, заголовок `Location`, поведение `[ApiController]`-валидации).

Запустить один тестовый проект:

```bash
dotnet test EventHub.Api/EventHub.Tests
dotnet test EventHub.Api/EventHub.IntegrationTests
```

Запустить один тест по имени:

```bash
dotnet test EventHub.Api/EventHub.Api.sln --filter "FullyQualifiedName~EventService_UpdateEvent"
dotnet test EventHub.Api/EventHub.Api.sln --filter "FullyQualifiedName~BookingServiceTests"
```

## Модель данных

### Event

| Поле          | Тип       | Обязательность | Описание                          |
|---------------|-----------|-----------------|------------------------------------|
| `Id`          | `Guid`    | генерируется сервером | Идентификатор события        |
| `Title`       | `string`  | обязательно     | Название события                   |
| `Description` | `string?` | опционально     | Описание события                   |
| `StartAt`     | `DateTime`| обязательно     | Дата и время начала                |
| `EndAt`       | `DateTime`| обязательно, позже `StartAt` | Дата и время окончания |

### Валидация

При создании и обновлении события выполняется проверка:

- `Title` не должен быть пустым;
- `StartAt` и `EndAt` обязательны;
- `EndAt` должен быть позже `StartAt`.

Проверка периода (`EndAt > StartAt`) продублирована в самом домене (`Event.ValidatePeriod`, вызывается и из конструктора, и из `UpdateDetails`) — так инвариант защищён независимо от того, идёт вызов через HTTP-DTO или нет.

Если валидация не прошла, API возвращает `400 Bad Request` с описанием ошибок.

### Booking

| Поле          | Тип             | Обязательность          | Описание                          |
|---------------|-----------------|--------------------------|------------------------------------|
| `Id`          | `Guid`          | генерируется сервером    | Идентификатор брони                |
| `EventId`     | `Guid`          | обязательно               | Событие, к которому относится бронь |
| `Status`      | `BookingStatus` | генерируется сервером    | Текущий статус брони               |
| `CreatedAt`   | `DateTime`      | генерируется сервером    | Дата и время создания брони        |
| `ProcessedAt` | `DateTime?`     | заполняется при обработке | Дата и время смены статуса         |

`BookingStatus`: `Pending` → `Confirmed` / `Rejected`. Смена статуса происходит только через доменные методы `Booking.Confirm()` / `Booking.Reject()`, которые атомарно проставляют `Status` и `ProcessedAt` — напрямую поле `ProcessedAt` не изменяется.

Бронь создаётся только для существующего и не удалённого события — `BookingService.CreateBookingAsync` сам проверяет событие через `IEventService` и возвращает `404 Not Found`, если событие не найдено или было удалено.

## Эндпоинты

### События — `/api/events`

| Метод  | Путь              | Описание                          | Успех            | Ошибка                    |
|--------|-------------------|-------------------------------------|------------------|----------------------------|
| GET    | `/events`         | Получить список событий (с фильтрацией и пагинацией) | `200 OK` | `400 Bad Request`  |
| GET    | `/events/{id}`    | Получить событие по `id`            | `200 OK`         | `404 Not Found`            |
| POST   | `/events`         | Создать новое событие               | `201 Created`    | `400 Bad Request`          |
| PUT    | `/events/{id}`    | Обновить событие целиком            | `200 OK`         | `404 Not Found` / `400 Bad Request` |
| DELETE | `/events/{id}`    | Удалить событие                     | `204 No Content` | `404 Not Found`         |
| POST   | `/events/{id}/book` | Создать бронь для события         | `202 Accepted`   | `404 Not Found`            |

### Бронирования — `/api/bookings`

| Метод | Путь            | Описание             | Успех    | Ошибка         |
|-------|-----------------|------------------------|----------|----------------|
| GET   | `/bookings/{id}`| Получить бронь по `id` | `200 OK` | `404 Not Found`|

`POST /api/events/{id}/book` возвращает `202 Accepted` (а не `201 Created`) — бронь создаётся синхронно, но её обработка (подтверждение/отклонение) выполняется асинхронно фоновым сервисом, поэтому на момент ответа бронь ещё в статусе `Pending`. Ответ содержит заголовок `Location`, указывающий на `GET /api/bookings/{id}` — по нему можно отследить итоговый статус.

### Примеры запросов

**Получить события с фильтрацией и пагинацией**

```http
GET /api/events?title=митинг&from=2026-07-01T00:00:00&to=2026-07-31T23:59:59&page=1&pageSize=10
```

Параметры query-строки (все опциональны, кроме `page`/`pageSize`, у которых есть значения по умолчанию):

| Параметр   | Тип        | По умолчанию | Описание                                              |
|------------|------------|--------------|--------------------------------------------------------|
| `title`    | `string?`  | —            | Частичный, регистронезависимый поиск по названию       |
| `from`     | `DateTime?`| —            | Событие начинается не раньше указанной даты (`StartAt >= from`) |
| `to`       | `DateTime?`| —            | Событие заканчивается не позже указанной даты (`EndAt <= to`)   |
| `page`     | `int`      | `1`          | Номер страницы, должен быть `>= 1`                      |
| `pageSize` | `int`      | `10`         | Размер страницы, должен быть `>= 1`                     |

При `page < 1` или `pageSize < 1` API возвращает `400 Bad Request`.

**Создать событие**

```http
POST /api/events
Content-Type: application/json

{
  "title": "Митинг команды",
  "description": "Еженедельный синк",
  "startAt": "2026-07-20T10:00:00",
  "endAt": "2026-07-20T11:00:00"
}
```

**Обновить событие**

```http
PUT /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "title": "Митинг команды (перенесён)",
  "description": "Еженедельный синк",
  "startAt": "2026-07-21T10:00:00",
  "endAt": "2026-07-21T11:00:00"
}
```

**Удалить событие**

```http
DELETE /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Создать бронь для события**

```http
POST /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
```

Ответ `202 Accepted` с заголовком `Location: /api/bookings/{bookingId}` и телом:

```json
{
  "data": {
    "id": "b1f2c3d4-...",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Pending",
    "createdAt": "2026-08-12T10:00:00Z",
    "processedAt": null
  },
  "success": true,
  "statusCode": "Accepted",
  "dateTime": "2026-08-12T10:00:00Z",
  "message": "Бронь принята в обработку, статус можно отследить по Location"
}
```

**Получить бронь по id**

```http
GET /api/bookings/b1f2c3d4-...
```

## Фоновая обработка бронирований

`BookingProcessingBackgroundService` (`Services/BookingProcessingBackgroundService.cs`) — `BackgroundService`, зарегистрированный через `AddHostedService` в `Program.cs`. Работает в фоне на протяжении всего времени жизни приложения:

1. каждые 5 секунд опрашивает `BookingService.GetPendingBookingsAsync()` на наличие броней в статусе `Pending`;
2. для каждой найденной брони выполняет `Task.Delay` на 2 секунды — имитация обращения к внешней системе (например, к платёжному шлюзу или системе подтверждения мест);
3. переводит бронь в статус `Confirmed` через `booking.Confirm()` (заполняет `ProcessedAt`);
4. сохраняет обновлённую бронь через `BookingService.UpdateBookingAsync()`.

> В текущем спринте бронь всегда подтверждается (`Confirm()`). Логика выбора между `Confirm()`/`Reject()` — предмет следующих спринтов.

Поскольку хранилище броней (статический `List<Booking>`) теперь одновременно читается и изменяется и из HTTP-запросов, и из фонового потока, доступ к нему в `BookingService` защищён `lock`.

## Формат ответа и обработка ошибок

Все ответы API оборачиваются в единый конверт (`Contracts/ApiResult.cs`):

```json
{
  "data": { ... },
  "success": true,
  "statusCode": "OK",
  "dateTime": "2026-08-12T10:00:00Z",
  "message": "..."
}
```

`statusCode` в теле всегда совпадает с реальным HTTP-статусом ответа — это гарантируется классами `ApiResultActionResult`/`ApiResultWithLocationResult` (`Common/`), которые оба берут статус из `ApiBaseResult.StatusCode`, а не выставляют его отдельно. Контроллеры не используют `Ok()`/`CreatedAtAction()`/etc. напрямую — только `response.ToActionResult()` / `response.ToActionResultWithLocation(...)`.

Ошибки обрабатываются централизованно в `GlobalExceptionHandlingMiddleware`. Доменные исключения наследуются от `ApiException` (`Common/Exceptions/ApiException.cs`) и сами несут свой HTTP-статус:

| Исключение             | HTTP-статус | Когда бросается                                  |
|-------------------------|-------------|---------------------------------------------------|
| `NotFoundException`     | `404`       | Событие/бронь не найдены                          |
| `BadRequestException`   | `400`       | Некорректные параметры запроса (например, `page`/`pageSize < 1`) |

Для непредвиденных исключений (не `ApiException`) middleware возвращает `500` и не пробрасывает `ex.Message` в тело ответа (только в лог) — чтобы не раскрывать детали реализации клиенту.

## Структура проекта

```
EventHub.Api/
├── Controllers/
│   ├── EventsController.cs
│   └── BookingsController.cs
├── Models/
│   ├── Event/
│   │   ├── Event.cs
│   │   └── EventFilter.cs
│   └── Booking/
│       ├── Booking.cs
│       └── BookingStatus.cs
├── Contracts/
│   ├── ApiResult.cs
│   ├── PaginatedResult.cs
│   ├── Event/
│   │   └── EventDto.cs
│   └── Booking/
│       └── BookingDto.cs
├── Services/
│   ├── IEventService.cs
│   ├── EventService.cs
│   ├── IBookingService.cs
│   ├── BookingService.cs
│   └── BookingProcessingBackgroundService.cs
├── Common/
│   ├── ApiResultActionResult.cs
│   ├── ApiResultWithLocationResult.cs
│   ├── ApiBaseResultExtensions.cs
│   ├── Exceptions/
│   │   ├── ApiException.cs
│   │   ├── NotFoundException.cs
│   │   └── BadRequestException.cs
│   ├── Extensions/
│   │   ├── Event/
│   │   └── Booking/
│   └── Middlewares/
│       └── GlobalExceptionHandlingMiddleware.cs
└── Program.cs
```
