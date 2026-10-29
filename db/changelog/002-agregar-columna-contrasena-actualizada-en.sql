--liquibase formatted sql

-- =============================================================================
-- Migración 002 - Adición de columna
-- HU-027 Recuperar contraseña: fecha del último cambio de contraseña. Permite
-- rechazar cualquier token emitido antes de ese momento.
-- Es NULL para no afectar a los usuarios que ya existen; Entity Framework
-- ignora la columna mientras no se agregue al modelo, así que la API desplegada
-- sigue funcionando igual.
-- =============================================================================

--changeset bridged:002-agregar-columna-contrasena-actualizada-en labels:hu-027
--comment: Agrega usuario.contrasena_actualizada_en (HU-027)
ALTER TABLE dbo.usuario ADD contrasena_actualizada_en DATETIME2 NULL;
--rollback ALTER TABLE dbo.usuario DROP COLUMN contrasena_actualizada_en;
