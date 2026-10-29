--liquibase formatted sql

-- =============================================================================
-- Migración 000 - Línea base
-- Esquema que ya existía en Azure SQL antes de adoptar Liquibase (tablas,
-- índices, triggers y la vista vw_gastos_consolidados), tal como lo creó el
-- script original del equipo.
--
-- Comportamiento:
--   - En una base VACÍA (desarrollo, CI): la precondición se cumple (no existe
--     la tabla usuario) y el script crea todo el esquema.
--   - En una base que YA tiene el esquema (autivadb): la precondición falla y,
--     por onFail:MARK_RAN, Liquibase solo la registra como aplicada sin
--     ejecutar nada. Así no se tocan las tablas existentes.
--
-- endDelimiter:GO separa las sentencias en cada línea GO, para que los
-- cuerpos de los triggers (que llevan ";" dentro) no se partan.
--
-- IMPORTANTE: no modificar este archivo después de aplicarlo: Liquibase
-- detecta el cambio de checksum y se detiene.
-- =============================================================================

--changeset equipo-autiva:000-linea-base labels:linea-base endDelimiter:GO
--comment: Esquema inicial de Autiva (previo a Liquibase)
--preconditions onFail:MARK_RAN
--precondition-sql-check expectedResult:0 SELECT COUNT(*) FROM sys.tables WHERE name = 'usuario' AND schema_id = SCHEMA_ID('dbo')

/* =============================================================================
   AUTIVA - Expediente digital vehicular (PWA)
   Script de creación de la base de datos para Azure SQL Database
  
   CÓMO EJECUTARLO
     - Crear primero la base de datos desde el portal de Azure.
     - Conectarse a esa base (no a master) y ejecutar el script completo desde
       VS Code (extensión SQL Server / mssql) o SQL Server Management Studio.
     - El script usa separadores GO: cada vista y cada trigger debe ir en su
       propio lote.

   CONVENCIONES
     - Nombres en snake_case y en español, igual que en el diagrama ER.
     - Restricciones con nombre fijo:
         PK_ llave primaria     FK_ llave foránea     UQ_ valor único
         CK_ regla CHECK        DF_ valor por defecto
         IX_ índice             UX_ índice único      TR_ trigger
     - Fechas de auditoría en UTC (SYSUTCDATETIME). El frontend convierte a
       la hora local al mostrar.

   DECISIONES DE DISEÑO PARA LA PWA (OFFLINE-FIRST)
     - Llaves uniqueidentifier (GUID) generadas en el dispositivo: un registro
       capturado sin conexión ya tiene su ID definitivo y al sincronizar no
       choca con registros de otros dispositivos.
     - creado_en / actualizado_en / eliminado_en: permiten sincronización
       incremental ("dame lo que cambió desde mi última sincronización").
     - Borrado lógico (eliminado_en) en lugar de DELETE: así el borrado también
       llega a los demás dispositivos. Por eso las llaves foráneas NO usan
       ON DELETE CASCADE.
     - Los valores de lista fija (enums del diagrama) se guardan como texto
       con una restricción CHECK.
   ============================================================================= */

/* =============================================================================
   1. CATÁLOGOS
   -----------------------------------------------------------------------------
   Listas que administra el equipo, no el usuario. Usan tinyint porque son
   pocos valores, nunca se crean sin conexión y el dispositivo los guarda en
   caché. Se pueden agregar opciones sin modificar la estructura.
   ============================================================================= */

CREATE TABLE dbo.tipo_combustible (
    id      tinyint      NOT NULL,
    nombre  nvarchar(30) NOT NULL,
    CONSTRAINT PK_tipo_combustible        PRIMARY KEY (id),
    CONSTRAINT UQ_tipo_combustible_nombre UNIQUE (nombre)
);

CREATE TABLE dbo.tipo_servicio (
    id      tinyint      NOT NULL,
    nombre  nvarchar(50) NOT NULL,
    CONSTRAINT PK_tipo_servicio        PRIMARY KEY (id),
    CONSTRAINT UQ_tipo_servicio_nombre UNIQUE (nombre)
);

-- Solo categorías de gastos generales. Gasolina, mantenimiento, pólizas y
-- verificaciones tienen su propia tabla y se suman en vw_gastos_consolidados.
CREATE TABLE dbo.categoria_gasto (
    id      tinyint      NOT NULL,
    nombre  nvarchar(50) NOT NULL,
    CONSTRAINT PK_categoria_gasto        PRIMARY KEY (id),
    CONSTRAINT UQ_categoria_gasto_nombre UNIQUE (nombre)
);
GO


/* =============================================================================
   2. USUARIO Y CONFIGURACIÓN
   ============================================================================= */

-- Cuenta de acceso. El ID lo genera el servidor porque el registro siempre
-- se hace con conexión. La contraseña se guarda como hash, nunca en texto
-- plano. La intercalación de Azure SQL no distingue mayúsculas, por lo que
-- "Ana@mail.com" y "ana@mail.com" cuentan como el mismo correo.
CREATE TABLE dbo.usuario (
    id               uniqueidentifier NOT NULL CONSTRAINT DF_usuario_id             DEFAULT NEWID(),
    nombre           nvarchar(120)    NOT NULL,
    correo           nvarchar(150)    NOT NULL,
    contrasena_hash  nvarchar(255)    NOT NULL,
    tipo_cuenta      nvarchar(30)     NOT NULL CONSTRAINT DF_usuario_tipo_cuenta    DEFAULT N'estandar',
    creado_en        datetime2        NOT NULL CONSTRAINT DF_usuario_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en   datetime2        NOT NULL CONSTRAINT DF_usuario_actualizado_en DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_usuario        PRIMARY KEY (id),
    CONSTRAINT UQ_usuario_correo UNIQUE (correo)
);

-- Ajustes de la pantalla Perfil. Relación 1:1 con usuario: su llave primaria
-- es la misma que la llave foránea, así que no puede haber dos filas para el
-- mismo usuario.
CREATE TABLE dbo.preferencias_usuario (
    usuario_id             uniqueidentifier NOT NULL,
    notificaciones_push    bit              NOT NULL CONSTRAINT DF_preferencias_push          DEFAULT 1,
    unidad_medida          nvarchar(10)     NOT NULL CONSTRAINT DF_preferencias_unidad_medida DEFAULT N'km_l',
    respaldo_nube          bit              NOT NULL CONSTRAINT DF_preferencias_respaldo      DEFAULT 1,
    modo_offline           bit              NOT NULL CONSTRAINT DF_preferencias_offline       DEFAULT 1,
    ultima_sincronizacion  datetime2        NULL,
    CONSTRAINT PK_preferencias_usuario         PRIMARY KEY (usuario_id),
    CONSTRAINT FK_preferencias_usuario_usuario FOREIGN KEY (usuario_id) REFERENCES dbo.usuario (id),
    CONSTRAINT CK_preferencias_unidad_medida   CHECK (unidad_medida IN (N'km_l', N'mpg'))
);

-- Suscripciones de la Web Push API: lo que el servidor necesita para enviar
-- notificaciones aunque la app esté cerrada. Una por navegador o dispositivo
-- (relación 1:N con usuario).
CREATE TABLE dbo.suscripcion_push (
    id            uniqueidentifier NOT NULL CONSTRAINT DF_suscripcion_push_id        DEFAULT NEWID(),
    usuario_id    uniqueidentifier NOT NULL,
    endpoint      nvarchar(500)    NOT NULL,
    clave_p256dh  nvarchar(255)    NOT NULL,
    clave_auth    nvarchar(255)    NOT NULL,
    dispositivo   nvarchar(100)    NULL,
    activa        bit              NOT NULL CONSTRAINT DF_suscripcion_push_activa    DEFAULT 1,
    creado_en     datetime2        NOT NULL CONSTRAINT DF_suscripcion_push_creado_en DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_suscripcion_push          PRIMARY KEY (id),
    CONSTRAINT UQ_suscripcion_push_endpoint UNIQUE (endpoint),
    CONSTRAINT FK_suscripcion_push_usuario  FOREIGN KEY (usuario_id) REFERENCES dbo.usuario (id)
);

CREATE INDEX IX_suscripcion_push_usuario ON dbo.suscripcion_push (usuario_id);
GO


/* =============================================================================
   3. VEHÍCULO Y DOCUMENTOS
   ============================================================================= */

-- Centro del expediente: casi todas las tablas dependen de vehiculo (1:N).
-- El ID se genera en el dispositivo. La unicidad de placas y del vehículo
-- principal se controla con índices filtrados (sección 9) para que funcione
-- con el borrado lógico.
CREATE TABLE dbo.vehiculo (
    id                   uniqueidentifier NOT NULL,
    usuario_id           uniqueidentifier NOT NULL,
    marca                nvarchar(50)     NOT NULL,
    modelo               nvarchar(50)     NOT NULL,
    anio                 smallint         NOT NULL,
    placas               nvarchar(15)     NOT NULL,
    vin                  nvarchar(17)     NULL,
    color                nvarchar(30)     NULL,
    capacidad_tanque_l   decimal(6,2)     NULL,
    kilometraje_inicial  int              NOT NULL,
    odometro_actual      int              NOT NULL,
    estatus              nvarchar(20)     NOT NULL CONSTRAINT DF_vehiculo_estatus        DEFAULT N'activo',
    es_principal         bit              NOT NULL CONSTRAINT DF_vehiculo_es_principal   DEFAULT 0,
    foto_url             nvarchar(500)    NULL,
    notas_tecnicas       nvarchar(500)    NULL,
    creado_en            datetime2        NOT NULL CONSTRAINT DF_vehiculo_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en       datetime2        NOT NULL CONSTRAINT DF_vehiculo_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en         datetime2        NULL,
    CONSTRAINT PK_vehiculo          PRIMARY KEY (id),
    CONSTRAINT FK_vehiculo_usuario  FOREIGN KEY (usuario_id) REFERENCES dbo.usuario (id),
    CONSTRAINT CK_vehiculo_estatus  CHECK (estatus IN (N'activo', N'en_taller', N'inactivo')),
    CONSTRAINT CK_vehiculo_anio     CHECK (anio BETWEEN 1950 AND 2100),
    CONSTRAINT CK_vehiculo_km       CHECK (kilometraje_inicial >= 0 AND odometro_actual >= kilometraje_inicial),
    CONSTRAINT CK_vehiculo_tanque   CHECK (capacidad_tanque_l IS NULL OR capacidad_tanque_l > 0)
);

CREATE INDEX IX_vehiculo_usuario ON dbo.vehiculo (usuario_id);

-- Bóveda de documentos. Es el ÚNICO lugar donde se guardan archivos del
-- vehículo (incluida la póliza en PDF), para no duplicarlos. El archivo vive
-- en Azure Blob Storage; aquí solo se guarda su URL.
CREATE TABLE dbo.documento_vehicular (
    id              uniqueidentifier NOT NULL,
    vehiculo_id     uniqueidentifier NOT NULL,
    tipo            nvarchar(30)     NOT NULL,
    nombre_archivo  nvarchar(150)    NOT NULL,
    archivo_url     nvarchar(500)    NOT NULL,
    tamano_kb       int              NULL,
    vigencia        date             NULL,
    creado_en       datetime2        NOT NULL CONSTRAINT DF_documento_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en  datetime2        NOT NULL CONSTRAINT DF_documento_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en    datetime2        NULL,
    CONSTRAINT PK_documento_vehicular PRIMARY KEY (id),
    CONSTRAINT FK_documento_vehiculo  FOREIGN KEY (vehiculo_id) REFERENCES dbo.vehiculo (id),
    CONSTRAINT CK_documento_tipo      CHECK (tipo IN (N'tarjeta_circulacion', N'poliza', N'factura', N'verificacion', N'otro')),
    CONSTRAINT CK_documento_tamano    CHECK (tamano_kb IS NULL OR tamano_kb > 0)
);

CREATE INDEX IX_documento_vehiculo_tipo ON dbo.documento_vehicular (vehiculo_id, tipo);
GO


/* =============================================================================
   4. COMBUSTIBLE
   ============================================================================= */

-- Captura principal sin conexión (en la gasolinera).
-- El precio por litro NO se guarda: se calcula como costo_total / litros.
-- El rendimiento SÍ se guarda: depende de la carga anterior con tanque lleno
-- y recalcularlo con todo el historial en cada gráfica sería costoso. La API
-- lo calcula al guardar y lo recalcula en la carga siguiente si se edita o
-- borra una carga.
CREATE TABLE dbo.carga_combustible (
    id                   uniqueidentifier NOT NULL,
    vehiculo_id          uniqueidentifier NOT NULL,
    tipo_combustible_id  tinyint          NOT NULL,
    fecha_hora           datetime2        NOT NULL,   -- en UTC
    litros               decimal(6,2)     NOT NULL,
    costo_total          decimal(10,2)    NOT NULL,
    odometro             int              NOT NULL,
    tanque_lleno         bit              NOT NULL CONSTRAINT DF_carga_tanque_lleno   DEFAULT 1,
    rendimiento_kml      decimal(5,2)     NULL,
    gasolinera           nvarchar(100)    NULL,
    folio                nvarchar(50)     NULL,
    notas                nvarchar(300)    NULL,
    comprobante_url      nvarchar(500)    NULL,
    creado_en            datetime2        NOT NULL CONSTRAINT DF_carga_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en       datetime2        NOT NULL CONSTRAINT DF_carga_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en         datetime2        NULL,
    CONSTRAINT PK_carga_combustible   PRIMARY KEY (id),
    CONSTRAINT FK_carga_vehiculo      FOREIGN KEY (vehiculo_id)         REFERENCES dbo.vehiculo (id),
    CONSTRAINT FK_carga_tipo_combust  FOREIGN KEY (tipo_combustible_id) REFERENCES dbo.tipo_combustible (id),
    CONSTRAINT CK_carga_valores       CHECK (litros > 0 AND costo_total > 0 AND odometro >= 0),
    CONSTRAINT CK_carga_rendimiento   CHECK (rendimiento_kml IS NULL OR rendimiento_kml > 0)
);

CREATE INDEX IX_carga_vehiculo_fecha    ON dbo.carga_combustible (vehiculo_id, fecha_hora);
CREATE INDEX IX_carga_vehiculo_odometro ON dbo.carga_combustible (vehiculo_id, odometro);
GO


/* =============================================================================
   5. MANTENIMIENTO
   ============================================================================= */

-- Bitácora del taller. meta_proximo_km alimenta la barra "Próximo servicio"
-- del Dashboard y puede generar un recordatorio por kilometraje.
CREATE TABLE dbo.servicio_mantenimiento (
    id                    uniqueidentifier NOT NULL,
    vehiculo_id           uniqueidentifier NOT NULL,
    tipo_servicio_id      tinyint          NOT NULL,
    fecha                 date             NOT NULL,
    kilometraje_servicio  int              NOT NULL,
    costo                 decimal(10,2)    NOT NULL,
    meta_proximo_km       int              NULL,
    fecha_retorno         date             NULL,
    taller                nvarchar(120)    NULL,
    folio                 nvarchar(50)     NULL,
    notas                 nvarchar(500)    NULL,
    comprobante_url       nvarchar(500)    NULL,
    creado_en             datetime2        NOT NULL CONSTRAINT DF_servicio_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en        datetime2        NOT NULL CONSTRAINT DF_servicio_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en          datetime2        NULL,
    CONSTRAINT PK_servicio_mantenimiento PRIMARY KEY (id),
    CONSTRAINT FK_servicio_vehiculo      FOREIGN KEY (vehiculo_id)      REFERENCES dbo.vehiculo (id),
    CONSTRAINT FK_servicio_tipo          FOREIGN KEY (tipo_servicio_id) REFERENCES dbo.tipo_servicio (id),
    CONSTRAINT CK_servicio_valores       CHECK (costo >= 0 AND kilometraje_servicio >= 0),
    CONSTRAINT CK_servicio_meta_km       CHECK (meta_proximo_km IS NULL OR meta_proximo_km > kilometraje_servicio),
    CONSTRAINT CK_servicio_fecha_retorno CHECK (fecha_retorno IS NULL OR fecha_retorno >= fecha)
);

CREATE INDEX IX_servicio_vehiculo_fecha ON dbo.servicio_mantenimiento (vehiculo_id, fecha);
GO


/* =============================================================================
   6. TRÁMITES
   ============================================================================= */

-- Pólizas de seguro. Se guarda el historial, no solo la vigente (1:N).
-- costo = pago total. Si la póliza se paga en mensualidades, costo queda
-- vacío y cada pago se registra en la tabla gasto, para no contarlo dos veces.
CREATE TABLE dbo.poliza_seguro (
    id                   uniqueidentifier NOT NULL,
    vehiculo_id          uniqueidentifier NOT NULL,
    aseguradora          nvarchar(80)     NOT NULL,
    numero_poliza        nvarchar(50)     NOT NULL,
    fecha_inicio         date             NOT NULL,
    fecha_vencimiento    date             NOT NULL,
    cobertura            nvarchar(50)     NULL,
    deducible            decimal(10,2)    NULL,
    costo                decimal(10,2)    NULL,
    telefono_asistencia  nvarchar(20)     NULL,
    creado_en            datetime2        NOT NULL CONSTRAINT DF_poliza_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en       datetime2        NOT NULL CONSTRAINT DF_poliza_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en         datetime2        NULL,
    CONSTRAINT PK_poliza_seguro    PRIMARY KEY (id),
    CONSTRAINT FK_poliza_vehiculo  FOREIGN KEY (vehiculo_id) REFERENCES dbo.vehiculo (id),
    CONSTRAINT CK_poliza_fechas    CHECK (fecha_vencimiento > fecha_inicio),
    CONSTRAINT CK_poliza_montos    CHECK ((costo IS NULL OR costo > 0) AND (deducible IS NULL OR deducible >= 0))
);

CREATE INDEX IX_poliza_vehiculo_venc ON dbo.poliza_seguro (vehiculo_id, fecha_vencimiento);

-- Verificación ambiental. También guarda el historial.
CREATE TABLE dbo.verificacion_vehicular (
    id                  uniqueidentifier NOT NULL,
    vehiculo_id         uniqueidentifier NOT NULL,
    holograma           nvarchar(5)      NOT NULL,
    engomado            nvarchar(20)     NULL,
    periodo             nvarchar(30)     NULL,   -- ej. 'Octubre - Noviembre'
    fecha_verificacion  date             NULL,
    proxima_fecha       date             NULL,
    verificentro        nvarchar(100)    NULL,
    costo               decimal(10,2)    NULL,
    creado_en           datetime2        NOT NULL CONSTRAINT DF_verificacion_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en      datetime2        NOT NULL CONSTRAINT DF_verificacion_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en        datetime2        NULL,
    CONSTRAINT PK_verificacion_vehicular PRIMARY KEY (id),
    CONSTRAINT FK_verificacion_vehiculo  FOREIGN KEY (vehiculo_id) REFERENCES dbo.vehiculo (id),
    CONSTRAINT CK_verificacion_holograma CHECK (holograma IN (N'00', N'0', N'1', N'2')),
    CONSTRAINT CK_verificacion_fechas    CHECK (proxima_fecha IS NULL OR fecha_verificacion IS NULL OR proxima_fecha > fecha_verificacion),
    CONSTRAINT CK_verificacion_costo     CHECK (costo IS NULL OR costo >= 0)
);

CREATE INDEX IX_verificacion_vehiculo ON dbo.verificacion_vehicular (vehiculo_id);
GO


/* =============================================================================
   7. GASTOS GENERALES
   ============================================================================= */

-- Solo gastos que NO tienen tabla propia: lavado, peajes, tenencia, multas,
-- accesorios, mensualidades del seguro, etc. Así ningún costo se guarda dos
-- veces. La pantalla de Gastos y las estadísticas leen de la vista
-- vw_gastos_consolidados (sección 11), que suma todas las tablas.
CREATE TABLE dbo.gasto (
    id               uniqueidentifier NOT NULL,
    vehiculo_id      uniqueidentifier NOT NULL,
    categoria_id     tinyint          NOT NULL,
    concepto         nvarchar(120)    NOT NULL,
    monto            decimal(10,2)    NOT NULL,
    fecha            date             NOT NULL,
    odometro         int              NULL,
    comprobante_url  nvarchar(500)    NULL,
    creado_en        datetime2        NOT NULL CONSTRAINT DF_gasto_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en   datetime2        NOT NULL CONSTRAINT DF_gasto_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en     datetime2        NULL,
    CONSTRAINT PK_gasto           PRIMARY KEY (id),
    CONSTRAINT FK_gasto_vehiculo  FOREIGN KEY (vehiculo_id)  REFERENCES dbo.vehiculo (id),
    CONSTRAINT FK_gasto_categoria FOREIGN KEY (categoria_id) REFERENCES dbo.categoria_gasto (id),
    CONSTRAINT CK_gasto_monto     CHECK (monto > 0),
    CONSTRAINT CK_gasto_odometro  CHECK (odometro IS NULL OR odometro >= 0)
);

CREATE INDEX IX_gasto_vehiculo_fecha ON dbo.gasto (vehiculo_id, fecha);
GO


/* =============================================================================
   8. RECORDATORIOS Y NOTIFICACIONES
   ============================================================================= */

-- Alertas por fecha o por kilometraje.
-- Tiene tres llaves foráneas opcionales (poliza_id, verificacion_id,
-- servicio_id) en lugar de un patrón polimórfico (entidad_tipo + entidad_id),
-- porque así la base de datos sí garantiza que el registro referenciado exista.
--
-- Reglas que se validan con CHECK:
--   CK_recordatorio_un_origen    máximo una de las tres llaves tiene valor
--                                (ninguna = recordatorio general, ej. licencia)
--   CK_recordatorio_tipo_origen  la llave usada corresponde al tipo
--   CK_recordatorio_criterio     por_fecha exige fecha_limite;
--                                por_kilometraje exige km_objetivo
--   CK_recordatorio_concluido    fecha_concluido solo existe si estado = concluido
--
-- El estado "vencido" no se guarda: se calcula comparando fecha_limite con la
-- fecha actual, para que nunca contradiga a la fecha.
CREATE TABLE dbo.recordatorio (
    id                 uniqueidentifier NOT NULL,
    vehiculo_id        uniqueidentifier NOT NULL,
    tipo               nvarchar(20)     NOT NULL,
    titulo             nvarchar(120)    NOT NULL,
    criterio           nvarchar(20)     NOT NULL,
    fecha_limite       date             NULL,
    km_objetivo        int              NULL,
    anticipacion_dias  tinyint          NOT NULL CONSTRAINT DF_recordatorio_anticipacion   DEFAULT 7,
    push_prioritaria   bit              NOT NULL CONSTRAINT DF_recordatorio_push           DEFAULT 0,
    estado             nvarchar(20)     NOT NULL CONSTRAINT DF_recordatorio_estado         DEFAULT N'pendiente',
    fecha_concluido    datetime2        NULL,
    poliza_id          uniqueidentifier NULL,
    verificacion_id    uniqueidentifier NULL,
    servicio_id        uniqueidentifier NULL,
    creado_en          datetime2        NOT NULL CONSTRAINT DF_recordatorio_creado_en      DEFAULT SYSUTCDATETIME(),
    actualizado_en     datetime2        NOT NULL CONSTRAINT DF_recordatorio_actualizado_en DEFAULT SYSUTCDATETIME(),
    eliminado_en       datetime2        NULL,
    CONSTRAINT PK_recordatorio              PRIMARY KEY (id),
    CONSTRAINT FK_recordatorio_vehiculo     FOREIGN KEY (vehiculo_id)     REFERENCES dbo.vehiculo (id),
    CONSTRAINT FK_recordatorio_poliza       FOREIGN KEY (poliza_id)       REFERENCES dbo.poliza_seguro (id),
    CONSTRAINT FK_recordatorio_verificacion FOREIGN KEY (verificacion_id) REFERENCES dbo.verificacion_vehicular (id),
    CONSTRAINT FK_recordatorio_servicio     FOREIGN KEY (servicio_id)     REFERENCES dbo.servicio_mantenimiento (id),
    CONSTRAINT CK_recordatorio_tipo         CHECK (tipo IN (N'seguro', N'verificacion', N'mantenimiento', N'tenencia', N'licencia', N'otro')),
    CONSTRAINT CK_recordatorio_criterio_val CHECK (criterio IN (N'por_fecha', N'por_kilometraje')),
    CONSTRAINT CK_recordatorio_estado       CHECK (estado IN (N'pendiente', N'notificado', N'concluido')),
    CONSTRAINT CK_recordatorio_anticipacion CHECK (anticipacion_dias IN (3, 7, 15, 30)),
    CONSTRAINT CK_recordatorio_km           CHECK (km_objetivo IS NULL OR km_objetivo > 0),
    CONSTRAINT CK_recordatorio_un_origen    CHECK (
          (CASE WHEN poliza_id       IS NOT NULL THEN 1 ELSE 0 END
         + CASE WHEN verificacion_id IS NOT NULL THEN 1 ELSE 0 END
         + CASE WHEN servicio_id     IS NOT NULL THEN 1 ELSE 0 END) <= 1),
    CONSTRAINT CK_recordatorio_tipo_origen  CHECK (
          (poliza_id       IS NULL OR tipo = N'seguro')
      AND (verificacion_id IS NULL OR tipo = N'verificacion')
      AND (servicio_id     IS NULL OR tipo = N'mantenimiento')),
    CONSTRAINT CK_recordatorio_criterio     CHECK (
          (criterio = N'por_fecha'       AND fecha_limite IS NOT NULL)
       OR (criterio = N'por_kilometraje' AND km_objetivo  IS NOT NULL)),
    CONSTRAINT CK_recordatorio_concluido    CHECK (
          (estado =  N'concluido' AND fecha_concluido IS NOT NULL)
       OR (estado <> N'concluido' AND fecha_concluido IS NULL))
);

CREATE INDEX IX_recordatorio_vehiculo_estado ON dbo.recordatorio (vehiculo_id, estado);
CREATE INDEX IX_recordatorio_estado_fecha    ON dbo.recordatorio (estado, fecha_limite);

-- Canales por los que se avisa cada recordatorio. Un recordatorio puede tener
-- varios canales; guardarlos en una sola columna rompería la primera forma
-- normal. La llave primaria compuesta evita repetir el mismo canal.
CREATE TABLE dbo.recordatorio_canal (
    recordatorio_id  uniqueidentifier NOT NULL,
    canal            nvarchar(20)     NOT NULL,
    CONSTRAINT PK_recordatorio_canal              PRIMARY KEY (recordatorio_id, canal),
    CONSTRAINT FK_recordatorio_canal_recordatorio FOREIGN KEY (recordatorio_id) REFERENCES dbo.recordatorio (id),
    CONSTRAINT CK_recordatorio_canal_valor        CHECK (canal IN (N'push_movil', N'web', N'correo', N'whatsapp'))
);

-- Bandeja de la campanita del encabezado. Se liga al usuario (y no solo al
-- recordatorio) porque también puede haber avisos del sistema que no vienen
-- de un recordatorio. Los crea siempre el servidor.
CREATE TABLE dbo.notificacion (
    id               uniqueidentifier NOT NULL CONSTRAINT DF_notificacion_id    DEFAULT NEWID(),
    usuario_id       uniqueidentifier NOT NULL,
    recordatorio_id  uniqueidentifier NULL,
    titulo           nvarchar(120)    NOT NULL,
    mensaje          nvarchar(300)    NOT NULL,
    fecha_envio      datetime2        NOT NULL CONSTRAINT DF_notificacion_envio DEFAULT SYSUTCDATETIME(),
    leida            bit              NOT NULL CONSTRAINT DF_notificacion_leida DEFAULT 0,
    CONSTRAINT PK_notificacion              PRIMARY KEY (id),
    CONSTRAINT FK_notificacion_usuario      FOREIGN KEY (usuario_id)      REFERENCES dbo.usuario (id),
    CONSTRAINT FK_notificacion_recordatorio FOREIGN KEY (recordatorio_id) REFERENCES dbo.recordatorio (id)
);

CREATE INDEX IX_notificacion_usuario_leida ON dbo.notificacion (usuario_id, leida);
CREATE INDEX IX_notificacion_recordatorio  ON dbo.notificacion (recordatorio_id);
GO


/* =============================================================================
   9. ÍNDICES ÚNICOS FILTRADOS
   -----------------------------------------------------------------------------
   Un UNIQUE normal impediría volver a registrar un vehículo después de
   borrarlo lógicamente, porque la fila borrada sigue en la tabla. El filtro
   WHERE eliminado_en IS NULL aplica la regla solo a los registros vigentes.
   ============================================================================= */

-- Un usuario no puede tener dos vehículos vigentes con las mismas placas.
CREATE UNIQUE INDEX UX_vehiculo_usuario_placas
    ON dbo.vehiculo (usuario_id, placas)
    WHERE eliminado_en IS NULL;

-- Un usuario solo puede tener un vehículo principal (el que muestra el Dashboard).
CREATE UNIQUE INDEX UX_vehiculo_principal
    ON dbo.vehiculo (usuario_id)
    WHERE es_principal = 1 AND eliminado_en IS NULL;
GO


/* =============================================================================
   10. TRIGGERS DE SINCRONIZACIÓN
   -----------------------------------------------------------------------------
   El DEFAULT de actualizado_en solo se aplica al insertar. Estos triggers lo
   actualizan en cada UPDATE con la hora del servidor, para que la
   sincronización incremental nunca pierda un cambio aunque la API olvide
   asignarlo o el reloj del dispositivo esté mal.

   IMPORTANTE para la API (.NET con Entity Framework Core 7 o superior):
   en tablas con triggers hay que declararlos en el DbContext, por ejemplo:
       modelBuilder.Entity<Vehiculo>()
           .ToTable(tb => tb.HasTrigger("TR_vehiculo_actualizado_en"));
   Si no se declaran, SaveChanges falla al actualizar esas tablas.
   ============================================================================= */

CREATE TRIGGER dbo.TR_usuario_actualizado_en ON dbo.usuario AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.usuario t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_vehiculo_actualizado_en ON dbo.vehiculo AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.vehiculo t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_documento_vehicular_actualizado_en ON dbo.documento_vehicular AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.documento_vehicular t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_carga_combustible_actualizado_en ON dbo.carga_combustible AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.carga_combustible t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_servicio_mantenimiento_actualizado_en ON dbo.servicio_mantenimiento AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.servicio_mantenimiento t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_poliza_seguro_actualizado_en ON dbo.poliza_seguro AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.poliza_seguro t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_verificacion_vehicular_actualizado_en ON dbo.verificacion_vehicular AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.verificacion_vehicular t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_gasto_actualizado_en ON dbo.gasto AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.gasto t JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER dbo.TR_recordatorio_actualizado_en ON dbo.recordatorio AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(@@PROCID) > 1 RETURN;
    UPDATE t SET actualizado_en = SYSUTCDATETIME()
    FROM dbo.recordatorio t JOIN inserted i ON t.id = i.id;
END;
GO


/* =============================================================================
   11. VISTA DE GASTOS CONSOLIDADOS
   -----------------------------------------------------------------------------
   Une en un solo listado todo lo que el usuario ha gastado en un vehículo,
   sin duplicar datos: cada costo vive en una sola tabla y aquí solo se suma.
   Excluye registros borrados lógicamente.

   Columnas: vehiculo_id, categoria, concepto, monto, fecha, odometro,
             origen (tabla de donde viene) y origen_id (ID del registro).

   Ejemplo - gasto por categoría desde una fecha:
       SELECT categoria, SUM(monto) AS total
       FROM dbo.vw_gastos_consolidados
       WHERE vehiculo_id = @vehiculo_id AND fecha >= @desde
       GROUP BY categoria;
   ============================================================================= */

CREATE VIEW dbo.vw_gastos_consolidados AS
-- Cargas de combustible
SELECT c.vehiculo_id,
       N'Gasolina'                                  AS categoria,
       COALESCE(c.gasolinera, N'Carga de combustible') AS concepto,
       c.costo_total                                AS monto,
       CAST(c.fecha_hora AS date)                   AS fecha,
       c.odometro                                   AS odometro,
       N'carga'                                     AS origen,
       c.id                                         AS origen_id
FROM dbo.carga_combustible c
WHERE c.eliminado_en IS NULL

UNION ALL
-- Servicios de mantenimiento
SELECT s.vehiculo_id, N'Mantenimiento', ts.nombre, s.costo, s.fecha,
       s.kilometraje_servicio, N'servicio', s.id
FROM dbo.servicio_mantenimiento s
JOIN dbo.tipo_servicio ts ON ts.id = s.tipo_servicio_id
WHERE s.eliminado_en IS NULL

UNION ALL
-- Pólizas pagadas de contado (las mensualidades están en gasto)
SELECT p.vehiculo_id, N'Seguros y trámites', CONCAT(N'Póliza ', p.aseguradora),
       p.costo, p.fecha_inicio, NULL, N'poliza', p.id
FROM dbo.poliza_seguro p
WHERE p.eliminado_en IS NULL AND p.costo IS NOT NULL

UNION ALL
-- Verificaciones realizadas con costo registrado
SELECT v.vehiculo_id, N'Seguros y trámites', N'Verificación vehicular',
       v.costo, v.fecha_verificacion, NULL, N'verificacion', v.id
FROM dbo.verificacion_vehicular v
WHERE v.eliminado_en IS NULL AND v.costo IS NOT NULL AND v.fecha_verificacion IS NOT NULL

UNION ALL
-- Gastos generales
SELECT g.vehiculo_id, cg.nombre, g.concepto, g.monto, g.fecha,
       g.odometro, N'gasto', g.id
FROM dbo.gasto g
JOIN dbo.categoria_gasto cg ON cg.id = g.categoria_id
WHERE g.eliminado_en IS NULL;
GO


/* =============================================================================
   12. DATOS INICIALES DE CATÁLOGOS
   ============================================================================= */

INSERT INTO dbo.tipo_combustible (id, nombre) VALUES
    (1, N'Magna'),
    (2, N'Premium'),
    (3, N'Diésel');

INSERT INTO dbo.tipo_servicio (id, nombre) VALUES
    (1, N'Aceite y filtros'),
    (2, N'Frenos'),
    (3, N'Llantas'),
    (4, N'Afinación'),
    (5, N'Batería'),
    (6, N'Otro');

INSERT INTO dbo.categoria_gasto (id, nombre) VALUES
    (1, N'Lavado'),
    (2, N'Peajes y estacionamiento'),
    (3, N'Tenencia y trámites'),
    (4, N'Multas'),
    (5, N'Accesorios'),
    (6, N'Otro');
GO


/* =============================================================================
   13. DESCRIPCIÓN DE TABLAS
   -----------------------------------------------------------------------------
   Guarda la descripción de cada tabla dentro de la propia base de datos
   (propiedad MS_Description), visible desde SSMS y VS Code.
   ============================================================================= */

DECLARE @descripciones TABLE (tabla sysname, descripcion nvarchar(400));
INSERT INTO @descripciones VALUES
    (N'tipo_combustible',       N'Catálogo de tipos de combustible.'),
    (N'tipo_servicio',          N'Catálogo de tipos de servicio de mantenimiento.'),
    (N'categoria_gasto',        N'Catálogo de categorías de gastos generales.'),
    (N'usuario',                N'Cuenta de acceso a la aplicación.'),
    (N'preferencias_usuario',   N'Ajustes del usuario (1:1 con usuario).'),
    (N'suscripcion_push',       N'Suscripciones Web Push por dispositivo.'),
    (N'vehiculo',               N'Vehículo del usuario; centro del expediente.'),
    (N'documento_vehicular',    N'Bóveda de documentos del vehículo (URL en Blob Storage).'),
    (N'carga_combustible',      N'Cargas de combustible; captura principal sin conexión.'),
    (N'servicio_mantenimiento', N'Bitácora de servicios de mantenimiento.'),
    (N'poliza_seguro',          N'Historial de pólizas de seguro.'),
    (N'verificacion_vehicular', N'Historial de verificaciones ambientales.'),
    (N'gasto',                  N'Gastos generales sin tabla propia.'),
    (N'recordatorio',           N'Recordatorios por fecha o kilometraje.'),
    (N'recordatorio_canal',     N'Canales de notificación de cada recordatorio.'),
    (N'notificacion',           N'Bandeja de notificaciones del usuario.');

DECLARE @tabla sysname, @descripcion nvarchar(400);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT tabla, descripcion FROM @descripciones;
OPEN c;
FETCH NEXT FROM c INTO @tabla, @descripcion;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sys.sp_addextendedproperty
        @name = N'MS_Description', @value = @descripcion,
        @level0type = N'SCHEMA', @level0name = N'dbo',
        @level1type = N'TABLE',  @level1name = @tabla;
    FETCH NEXT FROM c INTO @tabla, @descripcion;
END;
CLOSE c;
DEALLOCATE c;
GO

/* ============================== FIN DEL SCRIPT ============================== */


--rollback not required
