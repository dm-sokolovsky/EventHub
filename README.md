# EventHub

Платформа бронирования мест на события, разделённая на три независимых микросервиса. Регистрация и вход — только в сервисе пользователей (JWT), события и бронирования общаются асинхронно через Kafka. Хранение данных — PostgreSQL, у каждого сервиса своя база. Чтение событий кэшируется в Redis (cache-aside).

## Стек

- ASP.NET Core 10, minimal API (контроллеров нет)
- Чистая архитектура: каждый сервис — слои `Domain` ← `Application` ← `Infrastructure` ← `Api`
- PostgreSQL + EF Core 10, миграции применяются автоматически на старте (`Database.Migrate()`)
- Kafka (`Confluent.Kafka`): событийная интеграция между сервисами, JSON-сообщения
- Redis (`StackExchange.Redis`): кэширование чтения событий в EventsService (cache-aside)
- JWT Bearer-аутентификация; токен выдаёт UsersService, проверяют EventsService и BookingsService
- `BackgroundService`: обработка бронирований, Kafka-консьюмеры, инициализация топиков
- Ошибки — доменные исключения → `ProblemDetails` через `IExceptionHandler`
- Swagger UI поверх встроенной OpenAPI-спеки (только в Development)
- Docker Compose: 3 × Postgres + Kafka (ZooKeeper) + Redis + 3 сервиса

## Архитектура

```
┌────────────────┐   JWT (выдаёт)   ┌──────────────────┐
│  UsersService  │ ───────────────► │ токен проверяют: │
│  регистрация,  │                  │ Events, Bookings │
│  вход, JWT     │                  └──────────────────┘
└───────┬────────┘
        │ users-db

┌────────────────┐  events-changed  ┌─────────────────┐
│ EventsService  │ ───────────────► │ BookingsService │
│  CRUD событий, │                  │  брони, локальная│
│  места         │ ◄─────────────── │  копия событий  │
└───────┬────────┘  booking-confirmed└───────┬─────────┘
        │ events-db, redis                   │ bookings-db
                  Kafka (zookeeper)
```

Три сервиса, в каждом четыре проекта:

| Сервис | Ответственность | БД |
|--------|-----------------|----|
| `UsersService` | Регистрация, вход, хеширование паролей, выдача JWT; роли `User`/`Admin`; сидирование админа | `users` |
| `EventsService` | CRUD событий, учёт `AvailableSeats`; кэш чтения в Redis; подписка на `booking-confirmed` | `events` |
| `BookingsService` | Создание и отмена броней, фоновое подтверждение; подписка на `events-changed`, локальная копия событий | `bookings` |

**`EventHub.Contracts`** — разделяемая библиотека с межсервисными контрактами: record'ы сообщений (`BookingConfirmed`, `EventChanged`, `EventSnapshot`) и константы топиков (`Topics.BookingConfirmed`, `Topics.EventsChanged`). Издатель и подписчик ссылаются на одни и те же типы.

### Потоки Kafka

| Топик | Издатель | Подписчик | Содержимое |
|-------|----------|-----------|------------|
| `events-changed` | EventsService | BookingsService | `EventChanged` (Created/Updated/Deleted) — полный срез события |
| `booking-confirmed` | BookingsService | EventsService | `BookingConfirmed` (bookingId, eventId, userId, seats, confirmedAt) |

Ключ сообщения — идентификатор события: все сообщения по одному событию попадают в один partition и обрабатываются по порядку. Каждый сервис-подписчик использует свою группу потребителей (`Kafka:ConsumerGroup`: `events-service`, `bookings-service`).

Топики создаются при старте (`KafkaTopicInitializer` в обоих сервисах, админ-клиент Kafka, идемпотентно); ошибка создания не валит запуск.

### Локальная копия событий (BookingsService)

BookingsService не ходит в базу EventsService напрямую — связь только по идентификаторам и сообщениям. Консьюмер `EventsChangedConsumer` поддерживает таблицу `cd_local_events` (миграция `AddLocalEvents`), а `LocalEventsGateway` — реализация `IEventsGateway` поверх этой копии: проверка даты события, резервирование и освобождение мест при создании/отклонении брони.

### Кэширование чтения (EventsService)

Чтение событий работает по паттерну cache-aside поверх Redis:

- `GET /api/events/{id}` и `GET /api/events/top` сначала смотрят в кэш; при промахе данные читаются из PostgreSQL и записываются в кэш с TTL 5 минут;
- ключи: `event:{id}` для события, `event:top10` для топ-10 (сортировка по доле проданных мест: `(total_seats - available_seats) / total_seats`, считается в SQL);
- инвалидация: `PUT`/`DELETE` удаляют ключ события после сохранения в БД; `event:top10` явно не инвалидируется и живёт по TTL;
- кэш — оптимизация, а не часть операции: сбои Redis внутри `CacheService` перехватываются (`RedisException` → warning в лог), запрос деградирует до чтения из БД, а не падает с 500;
- в кэш пишется DTO `EventInfo`, а не доменная сущность (у `Event` приватные конструкторы — System.Text.Json её не десериализует).

Регистрация в `Program.cs`: `IConnectionMultiplexer` и `ICacheService` — singleton'ы. Строка подключения — `Redis:ConnectionString` в appsettings, в Docker перекрывается переменной `Redis__ConnectionString` (`redis:6379`).

## Быстрый старт через Docker

```bash
docker compose up -d --build
docker compose logs -f bookings-service     # наблюдать за консьюмерами
docker compose down                          # остановить (с удалением данных: docker compose down -v)
```

| Что | Адрес |
|-----|-------|
| Swagger UsersService | http://localhost:5001/swagger (корень редиректит сюда) |
| Swagger EventsService | http://localhost:5002/swagger |
| Swagger BookingsService | http://localhost:5003/swagger |
| PostgreSQL | `localhost:5433` (`users`), `5434` (`events`), `5435` (`bookings`); `postgres`/`postgres` |
| Kafka (с хоста) | `localhost:9092` |
| Redis | `localhost:6379` |

Важные детали compose-конфигурации:

- каждый сервис стартует после `service_healthy` своей базы и Kafka;
- строки подключения передаются переменными окружения (`ConnectionStrings__DefaultConnection`, `Kafka__BootstrapServers`, `Kafka__ConsumerGroup`, `Redis__ConnectionString`) — внутри сети Docker хосты `users-db`/`events-db`/`bookings-db`/`kafka:29092`/`redis:6379`, локально — `localhost`;
- у Kafka два listener'а: `kafka:29092` для контейнеров, `localhost:9092` для доступа с хоста;
- данные Postgres хранятся в именованных томах.

## Локальный запуск без Docker

Нужны локальные PostgreSQL (порты 5432, можно через compose: `docker compose up -d users-db events-db bookings-db`), Kafka (`localhost:9092`, например тот же compose: `docker compose up -d zookeeper kafka`) и Redis (`docker compose up -d redis`, порт 6379 проброшен на хост). Строки подключения в `appsettings.Development.json` каждого сервиса рассчитаны на `localhost`.

```bash
dotnet run --project src/UsersService.Api
dotnet run --project src/EventsService.Api
dotnet run --project src/BookingsService.Api
```

## Аутентификация и роли

Токен выдаёт только UsersService (`POST /api/user/auth/register`, `POST /api/user/auth/login`). EventsService и BookingsService проверяют подпись, издателя и аудиторию — значения секции `Jwt` (Issuer, Audience, Secret) во всех трёх сервисах должны совпадать, иначе токен не пройдёт проверку. В dev-конфигурации значения идентичны; для другого окружения задавайте через переменные окружения (`Jwt__Secret` и т.п.).

При старте UsersService сидирует администратора из секции `AdminSeed` (dev: логин `admin`, пароль `Admin123!`), если его ещё нет в базе.

Разграничение доступа:

| Эндпоинты | Требование |
|-----------|------------|
| `GET /api/events`, `GET /api/events/{id}` | анонимно (витрина) |
| `POST/PUT/DELETE /api/events` | роль `Admin` (политика `AdminOnly`) |
| брони (`book`, `GET /bookings`, `cancel`) | любой аутентифицированный пользователь; идентификатор пользователя читается из claim `sub`, отменять чужие брони может только `Admin` |

В Swagger UI авторизация настраивается кнопкой Authorize (схема Bearer) — вводится только токен, префикс `Bearer` добавляется автоматически.

## Эндпоинты

Все маршруты под префиксом `/api`. На защищённых маршрутах возможны `401` (нет/невалиден токен) и `403` (нет роли).

### UsersService — `/api/user/auth`

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/register` | Регистрация (`RegisterRequest`: login, password) → `201 Created` + `{ id }` (`409` при занятом логине) |
| POST | `/login` | Вход (`LoginRequest`) → `AuthResult` с `accessToken` |

### EventsService — `/api/events`

| Метод | Путь | Доступ | Описание |
|-------|------|--------|----------|
| GET | `/api/events` | анонимно | Список с фильтрами `title`/`from`/`to`; пагинация пока не проброшена — всегда первая страница по 10 элементов |
| GET | `/api/events/{id}` | анонимно | Событие по id (`404`, если нет) |
| GET | `/api/events/top` | анонимно | Топ-10 событий по доле проданных мест; ответ кэшируется |
| POST | `/api/events` | Admin | Создать (`201` + `Location`); публикует `EventChanged(Created)` |
| PUT | `/api/events/{id}` | Admin | Обновить; публикует `EventChanged(Updated)` |
| DELETE | `/api/events/{id}` | Admin | Удалить (`204`); публикует `EventChanged(Deleted)` |

### BookingsService — `/api`

| Метод | Путь | Доступ | Описание |
|-------|------|--------|----------|
| POST | `/events/{eventId}/book` | авторизован | Создать бронь (`Pending`); проверяет событие по локальной копии: дата в будущем, есть места, лимит 10 активных броней на пользователя |
| GET | `/bookings/{id}` | авторизован | Бронь по id |
| DELETE | `/bookings/{id}/cancel` | владелец или Admin | Отменить бронь (`InvalidBookingStateException`, если уже `Cancelled`/`Rejected`) |

Событие должно существовать в локальной копии BookingsService (появляется после создания в EventsService — задержка до доставки сообщения). До этого бронь по новому событию вернёт `404 Event not found`.

## Фоновая обработка и сквозной сценарий

`BookingProcessingBackgroundService` (BookingsService) каждые 5 секунд забирает `Pending`-брони и обрабатывает их параллельно (свой DI-scope на бронь, имитационная задержка 2 секунды):

1. событие не найдено в локальной копии → `Reject`, место не трогаем;
2. иначе `Confirm`, сохранение в БД, публикация `BookingConfirmed` в Kafka; ошибка Kafka (`EventPublishingException`) не отклоняет бронь — она остаётся `Pending` и будет повторена следующим тиком;
3. непредвиденная ошибка → `Reject` + возврат места в локальную копию.

В EventsService консьюмер `BookingConfirmedConsumer` по сообщению уменьшает `AvailableSeats` события на количество мест; событие не найдено или мест нет (в т.ч. дубликат сообщения) — пропуск с логированием, без падения.

Итого полный цикл: создание события админом → репликация в BookingsService → бронь пользователем → фоновое подтверждение (≈7 секунд) → списание мест в EventsService.

## Обработка ошибок

Доменные исключения (`NotFoundException`, `ValidationException`, `NoAvailableSeatsException`, `EventAlreadyStartedException`, `BookingLimitExceededException`, `BookingAccessDeniedException`, `InvalidBookingStateException`, `InvalidCredentialsException`) объявлены в `*Service.Domain/Exceptions`, ничего не знают про HTTP и превращаются в `ProblemDetails` в `GlobalExceptionHandler` каждого сервиса. Ошибки JWT (`OnChallenge`/`OnForbidden`) отдаются отдельным JSON-ответом (`401`/`403`).

## База данных и миграции

У каждого сервиса свой `AppDbContext` и свои миграции в `*Service.Infrastructure/Migrations`. Имена таблиц — snake_case с префиксом `cd_`. Ключевые таблицы:

| Сервис | Таблицы |
|--------|---------|
| UsersService | `Users` (пользователи, роль строкой) |
| EventsService | `cd_events` (события, `available_seats`) |
| BookingsService | `cd_bookings` (брони, связи только по `event_id`/`user_id`, без FK на другие базы), `cd_local_events` (локальная копия событий) |

`BookingStatus` хранится строкой. Связь между сервисами — только по идентификаторам, навигационных свойств и внешних ключей между базами нет.

Новая миграция (из корня репозитория):

```bash
dotnet ef migrations add <Name> --project src/BookingsService.Infrastructure --startup-project src/BookingsService.Api
```

## Тесты

```bash
dotnet test EventHub.slnx
```

- `tests/*Service.Tests` — юнит-тесты сервисов и доменных моделей; зависимости подменяются hand-written заглушками (`FakeEventPublisher`, `FakeCacheService`, `FakeEventRepository` в EventsService.Tests, `LocalEventsGateway` в BookingServiceTests); кэширование EventsService покрыто сценариями hit/miss/инвалидация в `EventServiceCacheTests`. DI в `EventServiceTests` собирается вручную, поэтому новые зависимости конструкторов нужно регистрировать и там;
- `tests/*Service.IntegrationTests` — тесты репозиториев и миграций на реальном PostgreSQL через Testcontainers (`postgres:16-alpine`); нужен только запущенный Docker.

## Структура репозитория

```
├── EventHub.slnx                     # решение
├── docker-compose.yml                # 3 × Postgres + ZooKeeper/Kafka + Redis + 3 сервиса
├── src/
│   ├── EventHub.Contracts/           # разделяемые контракты и топики Kafka
│   ├── UsersService.{Domain,Application,Infrastructure,Api}
│   ├── EventsService.{Domain,Application,Infrastructure,Api}
│   └── BookingsService.{Domain,Application,Infrastructure,Api}
└── tests/
    ├── UsersService.{Tests,IntegrationTests}
    ├── EventsService.{Tests,IntegrationTests}
    └── BookingsService.{Tests,IntegrationTests}
```
