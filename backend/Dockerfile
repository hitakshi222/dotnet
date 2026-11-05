FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/api.csproj backend/
RUN dotnet restore backend/api.csproj

COPY backend/ backend/
RUN dotnet publish backend/api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY postgres/schema.sql /app/database/schema.sql
COPY postgres/seed.sql /app/database/seed.sql
COPY backend/start-render.sh /usr/local/bin/start-render.sh
RUN chmod +x /usr/local/bin/start-render.sh

EXPOSE 10000
ENTRYPOINT ["/usr/local/bin/start-render.sh"]
