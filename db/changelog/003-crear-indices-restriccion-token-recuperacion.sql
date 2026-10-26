--liquibase formatted sql

-- =============================================================================
-- Migración 003 - Creación de índices y restricción
-- HU-027 Recuperar contraseña:
--   - UX_token_recuperacion_hash: el endpoint POST /api/auth/restablecer-contrasena
--     busca por hash del token; el índice único acelera la búsqueda e impide
--     que dos registros compartan el mismo token.
--   - IX_token_recuperacion_usuario: acelera invalidar los tokens de un usuario.
--   - CK_token_recuperacion_expira: la expiración debe ser posterior a la creación.
-- El rollback deshace los cambios en orden inverso.
-- =============================================================================

--changeset bridged:003-crear-indices-restriccion-token-recuperacion labels:hu-027
--comment: Índice único del hash, índice por usuario y CHECK de expiración (HU-027)
CREATE UNIQUE INDEX UX_token_recuperacion_hash ON dbo.token_recuperacion (token_hash);
CREATE INDEX IX_token_recuperacion_usuario ON dbo.token_recuperacion (usuario_id);
ALTER TABLE dbo.token_recuperacion ADD CONSTRAINT CK_token_recuperacion_expira CHECK (expira_en > creado_en);
--rollback ALTER TABLE dbo.token_recuperacion DROP CONSTRAINT CK_token_recuperacion_expira;
--rollback DROP INDEX IX_token_recuperacion_usuario ON dbo.token_recuperacion;
--rollback DROP INDEX UX_token_recuperacion_hash ON dbo.token_recuperacion;
