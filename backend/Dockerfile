# Multi-stage build for the ASP.NET Core API.
# Build stage uses the full SDK; the runtime stage ships only the ASP.NET runtime
# so the published image stays small and is portable to a VPS unchanged (per
# ARCHITECTURE.md §2.3). SDK/runtime tags track .NET 10; global.json pins the
# exact feature band (10.0.300, rollForward=latestFeature).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the whole backend tree (bin/obj are excluded via .dockerignore so the
# Linux build is not polluted by Windows-host artifacts). Restore + publish only
# the API project graph — test projects are not needed in the image.
COPY . .
RUN dotnet restore src/CheapAnalysis.Api/CheapAnalysis.Api.csproj
RUN dotnet publish src/CheapAnalysis.Api/CheapAnalysis.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl backs the Compose healthcheck against /healthz.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# Listen on all interfaces inside the container; Compose publishes 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CheapAnalysis.Api.dll"]
