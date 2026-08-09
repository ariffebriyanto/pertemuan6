# =========================================================================
# Multi-Stage Dockerfile untuk Core Banking API (Pertemuan 9 - Docker Hardening)
# =========================================================================

# Stage 1: Build & Publish Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /src

# Copy file nuget.config & .csproj lalu restore dependencies secara efisien (Layer Caching)
COPY ["nuget.config", "./"]
COPY ["BankCoreApi/BankCoreApi.csproj", "BankCoreApi/"]
RUN dotnet restore "BankCoreApi/BankCoreApi.csproj" --configfile nuget.config

# Copy seluruh source code dan publish dalam mode Release
COPY . .
WORKDIR "/src/BankCoreApi"
RUN dotnet publish "BankCoreApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Stage (Kecil, Aman, & Optimized)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Port standar kontainer Web API
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Hardening Keamanan: Buat user Non-Root agar kontainer tidak berjalan sebagai Root
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build-env /app/publish .

# Health check internal kontainer (Pertemuan 9)
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "BankCoreApi.dll"]
