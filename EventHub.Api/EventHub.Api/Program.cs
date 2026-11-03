using System.Reflection;
using System.Text;
using EventHab.Application.Repositories;
using EventHab.Application.Services;
using EventHab.Application.Services.Abstractions;
using EventHub.Api.Common;
using EventHub.Api.Common.Middlewares;
using EventHub.Api.Endpoints;
using EventHub.Api.Extensions;
using EventHub.Api.Services;
using EventHub.Infrastructure;
using EventHub.Infrastructure.Options;
using EventHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Не задана строка подключения ConnectionStrings:DefaultConnection "
                           + "(appsettings.json или переменная окружения ConnectionStrings__DefaultConnection).");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen(options =>
{
    // Путь к XML-файлу с документацией
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
}); 

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

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddHostedService<BookingProcessingBackgroundService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => Encoding.UTF8.GetByteCount(o.Secret) >= 32, "Jwt: Secret must be at least 32 bytes.")
    .ValidateOnStart();

builder.Services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Open Api V1"));

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// Корень редиректит на Swagger UI, чтобы http://localhost:8080 открывался сразу.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapUserEndpoints();
app.MapEventEndpoints();
app.MapBookingEndpoints();

app.Run();
