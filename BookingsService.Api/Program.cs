using BookingsService.Api.Common;
using BookingsService.Api.Common.Middlewares;
using BookingsService.Api.Endpoints;
using BookingsService.Api.Extensions;
using BookingsService.Api.Services;
using BookingsService.Application.Repositories;
using BookingsService.Application.Services;
using BookingsService.Application.Services.Abstractions;
using BookingsService.Infrastructure;
using BookingsService.Infrastructure.Gateways;
using BookingsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Не задана строка подключения ConnectionStrings:DefaultConnection "
                           + "(appsettings.json или переменная окружения ConnectionStrings__DefaultConnection).");

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 1. Инициализируем компоненты и коллекцию схем безопасности, если они null
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        // 2. Безопасно добавляем определение в коллекцию
        document.Components.SecuritySchemes.TryAdd("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Введите JWT токен без префикса Bearer"
        });

        // 3. Создаем схему безопасности (используя правильный конструктор и List<string>)
        var requirements = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", document),
                new List<string>()
            }
        };

        // 4. Применяем требование глобально ко всему документу
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(requirements);

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Заглушка EventsService: в памяти процесса, пока нет интеграции (Kafka)
builder.Services.AddSingleton<InMemoryEventsGateway>();
builder.Services.AddSingleton<IEventsGateway>(sp => sp.GetRequiredService<InMemoryEventsGateway>());

builder.Services.AddHostedService<BookingProcessingBackgroundService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

// Сидирование заглушки событий из конфигурации (только если задана секция EventsStub)
var stubEvents = builder.Configuration.GetSection("EventsStub:Events")
    .Get<List<StubEventEntry>>();
if (stubEvents is { Count: > 0 })
{
    var gateway = app.Services.GetRequiredService<InMemoryEventsGateway>();
    foreach (var stubEvent in stubEvents)
    {
        gateway.AddEvent(stubEvent.Id, stubEvent.StartAt, stubEvent.TotalSeats);
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Open Api V1"));
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// Корень редиректит на Swagger UI, чтобы http://localhost:8080 открывался сразу.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapBookingEndpoints();

app.Run();

internal sealed class StubEventEntry
{
    public Guid Id { get; init; }
    public DateTime StartAt { get; init; }
    public int TotalSeats { get; init; }
}
