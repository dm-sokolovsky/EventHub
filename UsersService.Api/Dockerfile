FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UsersService.Api/UsersService.Api.csproj", "UsersService.Api/"]
COPY ["UsersService.Application/UsersService.Application.csproj", "UsersService.Application/"]
COPY ["UsersService.Domain/UsersService.Domain.csproj", "UsersService.Domain/"]
COPY ["UsersService.Infrastructure/UsersService.Infrastructure.csproj", "UsersService.Infrastructure/"]
RUN dotnet restore "UsersService.Api/UsersService.Api.csproj"
COPY . .
WORKDIR "/src/UsersService.Api"
RUN dotnet build "./UsersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UsersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UsersService.Api.dll"]
