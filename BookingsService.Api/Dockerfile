FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookingsService.Api/BookingsService.Api.csproj", "BookingsService.Api/"]
COPY ["BookingsService.Application/BookingsService.Application.csproj", "BookingsService.Application/"]
COPY ["BookingsService.Domain/BookingsService.Domain.csproj", "BookingsService.Domain/"]
COPY ["BookingsService.Infrastructure/BookingsService.Infrastructure.csproj", "BookingsService.Infrastructure/"]
RUN dotnet restore "BookingsService.Api/BookingsService.Api.csproj"
COPY . .
WORKDIR "/src/BookingsService.Api"
RUN dotnet build "./BookingsService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookingsService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingsService.Api.dll"]
