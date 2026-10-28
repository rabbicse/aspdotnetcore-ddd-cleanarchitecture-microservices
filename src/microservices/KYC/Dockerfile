# syntax=docker/dockerfile:1
# Build context: this folder (src/microservices/KYC).  docker build -t kyc-api .

# ---- build ----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file or a package version changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Core/KYC.Domain/*.csproj src/Core/KYC.Domain/
COPY src/Application/KYC.Application/*.csproj src/Application/KYC.Application/
COPY src/Infrastructures/KYC.EventStore.EventStoreDB.Infrastructure/*.csproj src/Infrastructures/KYC.EventStore.EventStoreDB.Infrastructure/
COPY src/Infrastructures/KYC.Read.Mongo.Infrastructure/*.csproj src/Infrastructures/KYC.Read.Mongo.Infrastructure/
COPY src/Infrastructures/KYC.RedisCache.Infrastructure/*.csproj src/Infrastructures/KYC.RedisCache.Infrastructure/
COPY src/Infrastructures/KYC.Write.MsSql.Infrastructure/*.csproj src/Infrastructures/KYC.Write.MsSql.Infrastructure/
COPY src/Presentation/KYC.API/*.csproj src/Presentation/KYC.API/
RUN dotnet restore src/Presentation/KYC.API/KYC.API.csproj

COPY src/ src/
RUN dotnet publish src/Presentation/KYC.API/KYC.API.csproj \
      --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- runtime --------------------------------------------------------------------------------
# Debian-based image: the Kafka client (librdkafka) needs glibc, so do not switch to Alpine.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Docker \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# Run as the non-root "app" user that ships with the image.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "KYC.API.dll"]
