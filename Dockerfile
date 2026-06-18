# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY auth-service.csproj ./
RUN dotnet restore

# Copy source code and publish
COPY . ./
RUN dotnet publish auth-service.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security (Debian syntax)
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

COPY --from=build /app/publish ./

# Set ownership
RUN chown -R appuser:appgroup /app
USER appuser

# ASP.NET Core listens on port 8080 inside container
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "auth-service.dll"]
