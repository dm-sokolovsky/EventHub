FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BookingsService.Api/BookingsService.Api.csproj", "src/BookingsService.Api/"]
COPY ["src/BookingsService.Application/BookingsService.Application.csproj", "src/BookingsService.Application/"]
COPY ["src/BookingsService.Domain/BookingsService.Domain.csproj", "src/BookingsService.Domain/"]
COPY ["src/BookingsService.Infrastructure/BookingsService.Infrastructure.csproj", "src/BookingsService.Infrastructure/"]
RUN dotnet restore "src/BookingsService.Api/BookingsService.Api.csproj"
COPY . .
WORKDIR "/src/src/BookingsService.Api"
RUN dotnet build "./BookingsService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookingsService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingsService.Api.dll"]
