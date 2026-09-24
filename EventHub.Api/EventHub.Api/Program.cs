using System.Reflection;
using EventHub.Api.Common;
using EventHub.Api.DataAccess;
using EventHub.Api.DataAccess.Repositories;
using EventHub.Api.DataAccess.Repositories.Abstractions;
using EventHub.Api.Endpoints;
using EventHub.Api.Services;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddHostedService<BookingProcessingBackgroundService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

// Корень редиректит на Swagger UI, чтобы http://localhost:8080 открывался сразу.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapEventEndpoints();
app.MapBookingEndpoints();

app.Run();
