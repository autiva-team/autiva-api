# Autiva API — contexto para el agente

## Qué es Autiva
PWA de expediente digital vehicular: registro de cargas de combustible, servicios de mantenimiento, gastos, recordatorios y estadísticas por vehículo. Proyecto escolar (UTTT, Aplicaciones Web Progresivas) de un equipo de dos personas.

## Arquitectura
Cliente-servidor desacoplado. Este repo es solo el **backend**.
- **Backend (este repo):** API REST en ASP.NET, organizada en **Clean Architecture** con cuatro proyectos .NET 8 en la misma solución.
- **Frontend (otro repo, autiva-web):** SPA en React + Vite como PWA.
- **Base de datos:** Azure SQL Database, ya conectada con EF Core. Las tablas ya existen.
- **Despliegue:** Azure App Service. El workflow de GitHub Actions compila y publica `Autiva.Api/Autiva.Api.csproj` directamente (no el .sln), así que ese proyecto y su ruta no se mueven.

## Proyectos de la solución
```
Autiva.Domain/          entidades (Entities/), sin dependencias de nada más
Autiva.Application/     Dtos/, Interfaces/ (repositorios y servicios externos),
                         Services/ (lógica de negocio), DependencyInjection.cs
                         No referencia Entity Framework.
Autiva.Infrastructure/  Persistence/ (AutivaDbContext), Repositories/,
                         Security/ (hash de contraseña, generación de JWT),
                         DependencyInjection.cs
Autiva.Api/             Controllers/, Filters/, Program.cs
                         Los controladores solo llaman servicios de Application.
```

Referencias entre proyectos: `Application -> Domain`; `Infrastructure -> Application, Domain`; `Api -> Application, Infrastructure`.

En `Program.cs`: `builder.Services.AddApplication()` y `builder.Services.AddInfrastructure(builder.Configuration)` registran las dependencias de cada capa.

## Estructura de carpetas (dentro de Autiva.Api)
```
Controllers/     solo reciben la petición, validan y llaman al servicio
Filters/         filtros de Swagger (ej. AuthorizeOperationFilter)
Program.cs       inyección de dependencias, JWT, CORS, Swagger
```

Controladores (del catálogo de endpoints):

| Controlador | Ruta base |
|---|---|
| AuthController | /api/auth |
| UsuariosController | /api/usuarios/me |
| VehiculosController | /api/vehiculos |
| CargasController | /api/vehiculos/{vehiculoId}/cargas y /api/cargas |
| ServiciosController | /api/vehiculos/{vehiculoId}/servicios y /api/servicios |
| RecordatoriosController | /api/vehiculos/{vehiculoId}/recordatorios y /api/recordatorios |
| GastosController | /api/vehiculos/{vehiculoId}/gastos |
| EstadisticasController | /api/vehiculos/{vehiculoId}/estadisticas |
| PushController | /api/push |
| SincronizacionController | /api/sincronizacion |

## Reglas de diseño
- Los controladores no acceden directamente al DbContext; todo pasa por un servicio.
- El cálculo de rendimiento (km/L) vive en un `RendimientoService` compartido por cargas, dashboard y estadísticas.
- Autenticación con JWT; todos los endpoints, salvo registro, login y recuperación de contraseña, llevan `[Authorize]`.
- Cada usuario solo puede ver y modificar sus propios vehículos.
- **Sincronización offline:** `POST /api/sincronizacion` recibe en lote cargas y servicios, cada uno con un id generado en el cliente. Ese id tiene índice único para no guardar duplicados: si ya existe, se ignora sin error.
- **Push:** paquete NuGet `WebPush`. Las claves VAPID van en la configuración (appsettings / variables de App Service), nunca en el código. `RecordatoriosJob` es un `BackgroundService` con `PeriodicTimer`.
- CORS habilitado solo para el dominio del frontend (y `http://localhost:5173` en desarrollo).
- No modificar la conexión a la base ni borrar tablas existentes; si hace falta una columna nueva, avisar antes.

## Reglas de trabajo
- No crear ramas nuevas: trabajar en la rama que ya está activa.
- Mensajes de error y respuestas en español.
- Commits con el formato del catálogo, por ejemplo `feat(vehiculos): agregar alta de vehiculo (HU-004)`.
