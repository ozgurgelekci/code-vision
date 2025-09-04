# Railway Dockerfile for CodeVision
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["CodeVision.sln", "."]

# Copy project files
COPY ["CodeVision.API/CodeVision.API.csproj", "CodeVision.API/"]
COPY ["CodeVision.Core/CodeVision.Core.csproj", "CodeVision.Core/"]
COPY ["CodeVision.Infrastructure/CodeVision.Infrastructure.csproj", "CodeVision.Infrastructure/"]
COPY ["CodeVision.UI/CodeVision.UI.csproj", "CodeVision.UI/"]

# Restore all projects
RUN dotnet restore "CodeVision.sln"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/CodeVision.API"
RUN dotnet publish "CodeVision.API.csproj" -c Release -o /app/publish --no-restore

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway configuration
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "CodeVision.API.dll"]
