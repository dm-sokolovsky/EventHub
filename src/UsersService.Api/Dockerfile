FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/UsersService.Api/UsersService.Api.csproj", "src/UsersService.Api/"]
COPY ["src/UsersService.Application/UsersService.Application.csproj", "src/UsersService.Application/"]
COPY ["src/UsersService.Domain/UsersService.Domain.csproj", "src/UsersService.Domain/"]
COPY ["src/UsersService.Infrastructure/UsersService.Infrastructure.csproj", "src/UsersService.Infrastructure/"]
RUN dotnet restore "src/UsersService.Api/UsersService.Api.csproj"
COPY . .
WORKDIR "/src/src/UsersService.Api"
RUN dotnet build "./UsersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UsersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UsersService.Api.dll"]
