# Build context: repository root (needs Directory.*.props, global.json and all src projects).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, using only project files, so the layer is cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/InvoiceFlow.Domain/InvoiceFlow.Domain.csproj src/InvoiceFlow.Domain/
COPY src/InvoiceFlow.Application/InvoiceFlow.Application.csproj src/InvoiceFlow.Application/
COPY src/InvoiceFlow.Infrastructure/InvoiceFlow.Infrastructure.csproj src/InvoiceFlow.Infrastructure/
COPY src/InvoiceFlow.Api/InvoiceFlow.Api.csproj src/InvoiceFlow.Api/
RUN dotnet restore src/InvoiceFlow.Api/InvoiceFlow.Api.csproj

COPY src/ src/
RUN dotnet publish src/InvoiceFlow.Api/InvoiceFlow.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Fonts for QuestPDF and time zone data for "today in Portugal".
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-dejavu-core tzdata \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
# Writable directory for the signing key (mounted as a volume by docker compose).
RUN mkdir -p /app/keys && chown $APP_UID /app/keys
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InvoiceFlow.Api.dll"]
