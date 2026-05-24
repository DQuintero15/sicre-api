# SICRE API

API de gestión de cumplimiento regulatorio — .NET 10 / PostgreSQL.

## Dev

```bash
# Levantar base de datos
docker compose -f docker-compose.dev.yml up -d postgres

# Levantar todo (postgres + api)
docker compose -f docker-compose.dev.yml up -d

# Ver logs de la api
docker compose -f docker-compose.dev.yml logs -f api

# Bajar
docker compose -f docker-compose.dev.yml down

# Reljejar cambios en la api sin bajar postgres
docker compose -f docker-compose.dev.yml up -d --build api
```

Scalar UI disponible en `http://localhost:9001/scalar/v1` cuando corre en modo Development.

## Migraciones

```bash
cd Sicre.Api

# Crear migración
dotnet ef migrations add <NombreMigracion> --project Sicre.Api.csproj

# Aplicar migraciones
dotnet ef database update

# Revertir última migración
dotnet ef migrations remove
```

## Formato

El proyecto usa [CSharpier](https://csharpier.com) como formateador. Requiere instalación global una sola vez:

```bash
dotnet tool install -g csharpier
```

Para formatear todo el proyecto:

```bash
dotnet csharpier format .
```

## Build

```bash
# Compilar
dotnet build

# Ejecutar localmente (requiere postgres en :5433)
dotnet run --project Sicre.Api

# Publicar para producción
dotnet publish Sicre.Api -c Release -o ./publish
```

## Puertos

| Servicio  | Host   | Contenedor |
|-----------|--------|------------|
| API       | 9001   | 9001       |
| Postgres  | 5433   | 5432       |
