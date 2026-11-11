using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EventHub.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EventHub.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? throw new InvalidOperationException("Секция Jwt не найдена в конфигурации");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
                
                options.Events = new JwtBearerEvents
                {
                    // 1. Ошибка, если токен невалидный, просрочен или отсутствует (401 Unauthorized)
                    OnChallenge = async context =>
                    {
                        // Отменяем стандартный встроенный ответ
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new 
                        { 
                            error = "Вы не авторизованы", 
                            detail = context.ErrorDescription ?? "Токен отсутствует или недействителен" 
                        });

                        await context.Response.WriteAsync(result);
                    },

                    // 2. Ошибка, если токен валидный, но роль/политика не подходит (403 Forbidden)
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new 
                        { 
                            error = "Доступ запрещен", 
                            detail = "У вас недостаточно прав для выполнения этого действия" 
                        });

                        await context.Response.WriteAsync(result);
                    }
                };
            });
        
        services.AddAuthorization(options =>
        {
            // Политика только для администраторов
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin"));

            // Политика для любого авторизованного пользователя
            options.AddPolicy("AnyAuthenticated", policy => 
                policy.RequireAuthenticatedUser());
        });
        
        return services;
    }
}