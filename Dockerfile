# Railway Dockerfile for CodeVision
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE $PORT

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files
COPY ["CodeVision.API/CodeVision.API.csproj", "CodeVision.API/"]
COPY ["CodeVision.Core/CodeVision.Core.csproj", "CodeVision.Core/"]
COPY ["CodeVision.Infrastructure/CodeVision.Infrastructure.csproj", "CodeVision.Infrastructure/"]
COPY ["CodeVision.UI/CodeVision.UI.csproj", "CodeVision.UI/"]

# Restore packages
RUN dotnet restore "CodeVision.API/CodeVision.API.csproj"

# Copy source
COPY . .

# Build and publish API
WORKDIR "/src/CodeVision.API"
RUN dotnet build "CodeVision.API.csproj" -c Release -o /app/build --no-restore
RUN dotnet publish "CodeVision.API.csproj" -c Release -o /app/publish --no-restore --no-build

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway uses PORT environment variable
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CodeVision.API.dll"]
