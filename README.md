# autiva-api

Backend de Autiva, una PWA de expediente digital vehicular. API REST en ASP.NET 8 con arquitectura limpia, conectada a Azure SQL Database mediante Entity Framework Core.

| Proyecto | Contenido |
|---|---|
| `Autiva.Domain` | Entidades |
| `Autiva.Application` | DTOs, interfaces y servicios de negocio |
| `Autiva.Infrastructure` | `AutivaDbContext`, repositorios, seguridad (hash y JWT) |
| `Autiva.Api` | Controladores, filtros y `Program.cs` |
| `db/` | Migraciones de la base de datos (Liquibase) |

## Configuración local

Las credenciales nunca se guardan en el repositorio. En desarrollo se usan **User Secrets** (desde la carpeta `Autiva.Api`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena de conexión>"
dotnet user-secrets set "Jwt:Key" "<clave de al menos 32 caracteres>"
```

En Azure App Service se configuran como `ConnectionStrings__DefaultConnection` y `Jwt__Key`.

---

## Calidad de código: SonarQube Cloud

El análisis corre solo con GitHub Actions (`.github/workflows/sonarqube.yml`) en cada push a `develop`, en cada Pull Request y a mano desde la pestaña **Actions**. Resultados: <https://sonarcloud.io/project/overview?id=autiva-team_autiva-api>

### Configuración (una sola vez)

1. Entrar a <https://sonarcloud.io> con GitHub e importar la organización `autiva-team` (plan *Free*).
2. Instalar la app de SonarQube Cloud solo en `autiva-api` y `autiva-web`.
3. En el proyecto, **Administration → Analysis Method**: desactivar *Automatic Analysis* (el análisis lo hace el workflow, que compila la solución).
4. Generar un token en **My Account → Security** y guardarlo en GitHub como secreto `SONAR_TOKEN` (**Settings → Secrets and variables → Actions**).

### Qué hace el workflow

Instala Java 17 y .NET 8, instala `dotnet-sonarscanner` y compila la solución entre `begin` y `end` del escáner:

```bash
dotnet-sonarscanner begin /k:"autiva-team_autiva-api" /o:"autiva-team" /d:sonar.token="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io"
dotnet build Autiva.Api/Autiva.Api.sln --configuration Release
dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

### Ejecutarlo

- Automático: hacer push a `develop` o abrir un Pull Request. SonarQube Cloud comenta el resultado del Quality Gate en el PR.
- Manual: **Actions → Calidad de código (SonarQube Cloud) → Run workflow**.

---

## Versionamiento de la base de datos: Liquibase

Los cambios de estructura de la base se hacen **solo** con migraciones de Liquibase versionadas en `db/changelog/`. No se ejecutan scripts SQL a mano.

```
db/
├── changelog/
│   ├── db.changelog-master.yaml                          orden de las migraciones
│   ├── 000-linea-base.sql                                esquema previo a Liquibase
│   ├── 001-crear-tabla-token-recuperacion.sql            creación de tabla (HU-027)
│   ├── 002-agregar-columna-contrasena-actualizada-en.sql adición de columna (HU-027)
│   └── 003-crear-indices-restriccion-token-recuperacion.sql  índices y CHECK (HU-027)
└── liquibase.properties.example                          plantilla de configuración
```

Cada migración es un *changeset* de SQL con su bloque `--rollback`. Liquibase registra lo aplicado en la tabla `DATABASECHANGELOG` y usa `DATABASECHANGELOGLOCK` para que dos personas no migren a la vez.

### 1. Instalación (Windows)

1. Descargar el instalador **liquibase-windows-x64-installer-4.33.0.exe** desde <https://github.com/liquibase/liquibase/releases/tag/v4.33.0> e instalar con las opciones por defecto (incluye Java y el driver de SQL Server).
2. Abrir una terminal nueva y comprobar:

   ```powershell
   liquibase --version
   ```

> Se fija la versión 4.33.0 porque es la última con licencia Apache 2.0 que incluye el driver JDBC de SQL Server. Desde la 5.0 Liquibase Community no trae drivers.

### 2. Base de desarrollo

Las pruebas (sobre todo los rollback) se hacen **solo** en una base de desarrollo, nunca en `autivadb`. Se usa una segunda base gratuita en el mismo servidor de Azure SQL (la oferta gratuita permite hasta 10 por suscripción):

1. Azure Portal → servidor `autiva-sql-server-can` → **+ Create database** → nombre `autivadb-dev` → aplicar la oferta gratuita.
2. En **Networking** del servidor, agregar la IP propia (*Add your client IPv4 address*).

Como alternativa sin Azure, sirve un SQL Server local (Developer/Express o Docker) con TCP habilitado.

### 3. Configuración

```powershell
cd db
copy liquibase.properties.example liquibase.properties   # este archivo está en .gitignore
```

Editar `liquibase.properties` con el servidor, la base (`autivadb-dev`) y el usuario. La contraseña **no** va en el archivo: se define en la terminal, solo para esa sesión:

```powershell
$env:LIQUIBASE_COMMAND_PASSWORD = [System.Net.NetworkCredential]::new('', (Read-Host 'Contraseña SQL' -AsSecureString)).Password
```

### 4. Comandos (desde la carpeta `db`)

| Objetivo | Comando |
|---|---|
| Revisar que el changelog sea válido | `liquibase validate` |
| Ver migraciones **pendientes** | `liquibase status --verbose` |
| Ver el SQL que se va a ejecutar, sin ejecutarlo | `liquibase update-sql` |
| Aplicar todas las pendientes | `liquibase update` |
| Ver migraciones **aplicadas** (historial) | `liquibase history` |
| Ver el SQL de un rollback, sin ejecutarlo | `liquibase rollback-count-sql --count=1` |
| Revertir la última migración | `liquibase rollback-count --count=1` |

También se puede consultar el historial directamente en la base:

```sql
SELECT ORDEREXECUTED, ID, AUTHOR, FILENAME, DATEEXECUTED, EXECTYPE
FROM DATABASECHANGELOG
ORDER BY ORDEREXECUTED;
```

### 5. Reversión y reaplicación (solo en desarrollo)

```powershell
liquibase rollback-count-sql --count=1   # 1. revisar qué se va a deshacer
liquibase rollback-count --count=1       # 2. revertir la migración 003
liquibase status --verbose               # 3. la 003 aparece como pendiente
liquibase update                         # 4. reaplicarla
liquibase history                        # 5. confirmar que quedó aplicada
```

Para comprobar el efecto en la base:

```sql
SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.token_recuperacion') AND name LIKE '[UI]X[_]%'
UNION ALL
SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.token_recuperacion');
```

### 6. Ejecutar todas las migraciones desde una base limpia

1. Crear una base vacía (por ejemplo `autivadb-dev`, o borrar y volver a crear la de desarrollo).
2. Apuntar `liquibase.properties` a esa base y definir la contraseña.
3. Ejecutar:

   ```powershell
   liquibase status --verbose   # muestra 000, 001, 002 y 003 como pendientes
   liquibase update             # crea todo el esquema y aplica las migraciones
   liquibase history            # muestra las cuatro aplicadas
   ```

La línea base (`000`) tiene una precondición: si la tabla `usuario` **no** existe, crea el esquema completo; si **ya** existe, solo la marca como aplicada (`MARK_RAN`) sin tocar nada.

Esto mismo lo comprueba en automático el workflow `.github/workflows/migraciones-bd.yml` en cada push a `develop` o Pull Request que cambie `db/`: levanta un SQL Server temporal, aplica todo desde cero, revierte la última migración y la reaplica.

### 7. Aplicar en la base del proyecto (`autivadb`)

Solo después de probar en desarrollo:

1. Confirmar que existe un respaldo reciente (Azure SQL hace respaldos automáticos con restauración a un punto en el tiempo).
2. Apuntar `liquibase.properties` a `autivadb`.
3. `liquibase status --verbose` y `liquibase update-sql` para revisar.
4. `liquibase update`. La línea base se registra como `MARK_RAN` y se aplican 001 a 003.

En producción no se hacen pruebas de rollback.

### 8. Agregar una migración nueva

1. Crear `db/changelog/004-descripcion.sql` con este formato:

   ```sql
   --liquibase formatted sql

   --changeset <autor>:004-descripcion labels:hu-xxx
   --comment: Qué cambia y para qué historia de usuario
   <SQL del cambio>;
   --rollback <SQL que lo deshace>;
   ```

2. Agregarla al final de `db.changelog-master.yaml`.
3. Probar en desarrollo: `update`, `rollback-count --count=1` y `update`.
4. Subirla en un Pull Request. **Nunca** editar una migración que ya se aplicó.

Si el cambio debe usarse desde la API, actualizar después la entidad en `Autiva.Domain` y su configuración en `AutivaDbContext`.
