# EventHub

# EventHub.Api

Простой REST API для управления событиями (events) на ASP.NET Core.

## Стек

- ASP.NET Core Web API
- In-memory хранилище (статическая коллекция, без БД)
- DataAnnotations + `IValidatableObject` для валидации

## Запуск

```bash
dotnet restore
dotnet run --project EventHub.Api/EventHub.Api
```

По умолчанию API поднимется на `https://localhost:xxxx` (порт см. в `launchSettings.json` или в выводе консоли при старте). Swagger UI доступен по адресу `/swagger`, если подключён в проекте.

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

Если валидация не прошла, API возвращает `400 Bad Request` с описанием ошибок.

## Эндпоинты

Базовый путь: `/api/events`

| Метод  | Путь              | Описание                          | Успех            | Ошибка                    |
|--------|-------------------|-------------------------------------|------------------|----------------------------|
| GET    | `/events`         | Получить список всех событий        | `200 OK`         | —                          |
| GET    | `/events/{id}`    | Получить событие по `id`            | `200 OK`         | `404 Not Found`            |
| POST   | `/events`         | Создать новое событие               | `201 Created`    | `400 Bad Request`          |
| PUT    | `/events/{id}`    | Обновить событие целиком            | `200 OK`         | `404 Not Found` / `400 Bad Request` |
| DELETE | `/events/{id}`    | Удалить событие                     | `204 No Content` | `404 Not Found`         |

### Примеры запросов

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

## Структура проекта

```
EventHub.Api/
├── Controllers/
│   └── EventsController.cs
├── Models/
│   └── Event.cs
├── Services/
│   ├── IEventService.cs
│   └── EventService.cs
└── Program.cs
```
