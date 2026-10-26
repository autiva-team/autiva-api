--liquibase formatted sql

-- =============================================================================
-- Migración 001 - Creación de tabla
-- HU-027 Recuperar contraseña: guarda el token temporal que se envía por correo.
-- Se guarda el hash SHA-256 del token (64 caracteres hex), nunca el token en
-- texto plano, para que una filtración de la base no permita usar los enlaces.
-- =============================================================================

--changeset bridged:001-crear-tabla-token-recuperacion labels:hu-027
--comment: Crea la tabla token_recuperacion para la recuperación de contraseña (HU-027)
CREATE TABLE dbo.token_recuperacion (
    id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_token_recuperacion_id DEFAULT (NEWID()),
    usuario_id  UNIQUEIDENTIFIER NOT NULL,
    token_hash  CHAR(64)         NOT NULL,
    expira_en   DATETIME2        NOT NULL,
    usado_en    DATETIME2        NULL,
    creado_en   DATETIME2        NOT NULL CONSTRAINT DF_token_recuperacion_creado_en DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_token_recuperacion PRIMARY KEY (id),
    CONSTRAINT FK_token_recuperacion_usuario FOREIGN KEY (usuario_id)
        REFERENCES dbo.usuario (id)
);
--rollback DROP TABLE dbo.token_recuperacion;
