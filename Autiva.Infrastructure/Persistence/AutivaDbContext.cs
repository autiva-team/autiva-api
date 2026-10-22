using System;
using System.Collections.Generic;
using Autiva.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Persistence;

public partial class AutivaDbContext : DbContext
{
    public AutivaDbContext(DbContextOptions<AutivaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CargaCombustible> CargaCombustible { get; set; }

    public virtual DbSet<CategoriaGasto> CategoriaGasto { get; set; }

    public virtual DbSet<DocumentoVehicular> DocumentoVehicular { get; set; }

    public virtual DbSet<Gasto> Gasto { get; set; }

    public virtual DbSet<Notificacion> Notificacion { get; set; }

    public virtual DbSet<PolizaSeguro> PolizaSeguro { get; set; }

    public virtual DbSet<PreferenciasUsuario> PreferenciasUsuario { get; set; }

    public virtual DbSet<Recordatorio> Recordatorio { get; set; }

    public virtual DbSet<RecordatorioCanal> RecordatorioCanal { get; set; }

    public virtual DbSet<ServicioMantenimiento> ServicioMantenimiento { get; set; }

    public virtual DbSet<SuscripcionPush> SuscripcionPush { get; set; }

    public virtual DbSet<TipoCombustible> TipoCombustible { get; set; }

    public virtual DbSet<TipoServicio> TipoServicio { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<Vehiculo> Vehiculo { get; set; }

    public virtual DbSet<VerificacionVehicular> VerificacionVehicular { get; set; }

    public virtual DbSet<VwGastosConsolidados> VwGastosConsolidados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CargaCombustible>(entity =>
        {
            entity.ToTable("carga_combustible", tb =>
                {
                    tb.HasComment("Cargas de combustible; captura principal sin conexión.");
                    tb.HasTrigger("TR_carga_combustible_actualizado_en");
                });

            entity.HasIndex(e => new { e.VehiculoId, e.FechaHora }, "IX_carga_vehiculo_fecha");

            entity.HasIndex(e => new { e.VehiculoId, e.Odometro }, "IX_carga_vehiculo_odometro");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.ComprobanteUrl)
                .HasMaxLength(500)
                .HasColumnName("comprobante_url");
            entity.Property(e => e.CostoTotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("costo_total");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.FechaHora).HasColumnName("fecha_hora");
            entity.Property(e => e.Folio)
                .HasMaxLength(50)
                .HasColumnName("folio");
            entity.Property(e => e.Gasolinera)
                .HasMaxLength(100)
                .HasColumnName("gasolinera");
            entity.Property(e => e.Litros)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("litros");
            entity.Property(e => e.Notas)
                .HasMaxLength(300)
                .HasColumnName("notas");
            entity.Property(e => e.Odometro).HasColumnName("odometro");
            entity.Property(e => e.RendimientoKml)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("rendimiento_kml");
            entity.Property(e => e.TanqueLleno)
                .HasDefaultValue(true)
                .HasColumnName("tanque_lleno");
            entity.Property(e => e.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.TipoCombustible).WithMany(p => p.CargaCombustible)
                .HasForeignKey(d => d.TipoCombustibleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_carga_tipo_combust");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.CargaCombustible)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_carga_vehiculo");
        });

        modelBuilder.Entity<CategoriaGasto>(entity =>
        {
            entity.ToTable("categoria_gasto", tb => tb.HasComment("Catálogo de categorías de gastos generales."));

            entity.HasIndex(e => e.Nombre, "UQ_categoria_gasto_nombre").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<DocumentoVehicular>(entity =>
        {
            entity.ToTable("documento_vehicular", tb =>
                {
                    tb.HasComment("Bóveda de documentos del vehículo (URL en Blob Storage).");
                    tb.HasTrigger("TR_documento_vehicular_actualizado_en");
                });

            entity.HasIndex(e => new { e.VehiculoId, e.Tipo }, "IX_documento_vehiculo_tipo");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.ArchivoUrl)
                .HasMaxLength(500)
                .HasColumnName("archivo_url");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(150)
                .HasColumnName("nombre_archivo");
            entity.Property(e => e.TamanoKb).HasColumnName("tamano_kb");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.Vigencia).HasColumnName("vigencia");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.DocumentoVehicular)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_documento_vehiculo");
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.ToTable("gasto", tb =>
                {
                    tb.HasComment("Gastos generales sin tabla propia.");
                    tb.HasTrigger("TR_gasto_actualizado_en");
                });

            entity.HasIndex(e => new { e.VehiculoId, e.Fecha }, "IX_gasto_vehiculo_fecha");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.ComprobanteUrl)
                .HasMaxLength(500)
                .HasColumnName("comprobante_url");
            entity.Property(e => e.Concepto)
                .HasMaxLength(120)
                .HasColumnName("concepto");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Monto)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("monto");
            entity.Property(e => e.Odometro).HasColumnName("odometro");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Gasto)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_gasto_categoria");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Gasto)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_gasto_vehiculo");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.ToTable("notificacion", tb => tb.HasComment("Bandeja de notificaciones del usuario."));

            entity.HasIndex(e => e.RecordatorioId, "IX_notificacion_recordatorio");

            entity.HasIndex(e => new { e.UsuarioId, e.Leida }, "IX_notificacion_usuario_leida");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("fecha_envio");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(300)
                .HasColumnName("mensaje");
            entity.Property(e => e.RecordatorioId).HasColumnName("recordatorio_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(120)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Recordatorio).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.RecordatorioId)
                .HasConstraintName("FK_notificacion_recordatorio");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificacion)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_usuario");
        });

        modelBuilder.Entity<PolizaSeguro>(entity =>
        {
            entity.ToTable("poliza_seguro", tb =>
                {
                    tb.HasComment("Historial de pólizas de seguro.");
                    tb.HasTrigger("TR_poliza_seguro_actualizado_en");
                });

            entity.HasIndex(e => new { e.VehiculoId, e.FechaVencimiento }, "IX_poliza_vehiculo_venc");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Aseguradora)
                .HasMaxLength(80)
                .HasColumnName("aseguradora");
            entity.Property(e => e.Cobertura)
                .HasMaxLength(50)
                .HasColumnName("cobertura");
            entity.Property(e => e.Costo)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("costo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Deducible)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("deducible");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.NumeroPoliza)
                .HasMaxLength(50)
                .HasColumnName("numero_poliza");
            entity.Property(e => e.TelefonoAsistencia)
                .HasMaxLength(20)
                .HasColumnName("telefono_asistencia");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.PolizaSeguro)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_poliza_vehiculo");
        });

        modelBuilder.Entity<PreferenciasUsuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId);

            entity.ToTable("preferencias_usuario", tb => tb.HasComment("Ajustes del usuario (1:1 con usuario)."));

            entity.Property(e => e.UsuarioId)
                .ValueGeneratedNever()
                .HasColumnName("usuario_id");
            entity.Property(e => e.ModoOffline)
                .HasDefaultValue(true)
                .HasColumnName("modo_offline");
            entity.Property(e => e.NotificacionesPush)
                .HasDefaultValue(true)
                .HasColumnName("notificaciones_push");
            entity.Property(e => e.RespaldoNube)
                .HasDefaultValue(true)
                .HasColumnName("respaldo_nube");
            entity.Property(e => e.UltimaSincronizacion).HasColumnName("ultima_sincronizacion");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(10)
                .HasDefaultValue("km_l")
                .HasColumnName("unidad_medida");

            entity.HasOne(d => d.Usuario).WithOne(p => p.PreferenciasUsuario)
                .HasForeignKey<PreferenciasUsuario>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_preferencias_usuario_usuario");
        });

        modelBuilder.Entity<Recordatorio>(entity =>
        {
            entity.ToTable("recordatorio", tb =>
                {
                    tb.HasComment("Recordatorios por fecha o kilometraje.");
                    tb.HasTrigger("TR_recordatorio_actualizado_en");
                });

            entity.HasIndex(e => new { e.Estado, e.FechaLimite }, "IX_recordatorio_estado_fecha");

            entity.HasIndex(e => new { e.VehiculoId, e.Estado }, "IX_recordatorio_vehiculo_estado");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.AnticipacionDias)
                .HasDefaultValue((byte)7)
                .HasColumnName("anticipacion_dias");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Criterio)
                .HasMaxLength(20)
                .HasColumnName("criterio");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("pendiente")
                .HasColumnName("estado");
            entity.Property(e => e.FechaConcluido).HasColumnName("fecha_concluido");
            entity.Property(e => e.FechaLimite).HasColumnName("fecha_limite");
            entity.Property(e => e.KmObjetivo).HasColumnName("km_objetivo");
            entity.Property(e => e.PolizaId).HasColumnName("poliza_id");
            entity.Property(e => e.PushPrioritaria).HasColumnName("push_prioritaria");
            entity.Property(e => e.ServicioId).HasColumnName("servicio_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.Titulo)
                .HasMaxLength(120)
                .HasColumnName("titulo");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.VerificacionId).HasColumnName("verificacion_id");

            entity.HasOne(d => d.Poliza).WithMany(p => p.Recordatorio)
                .HasForeignKey(d => d.PolizaId)
                .HasConstraintName("FK_recordatorio_poliza");

            entity.HasOne(d => d.Servicio).WithMany(p => p.Recordatorio)
                .HasForeignKey(d => d.ServicioId)
                .HasConstraintName("FK_recordatorio_servicio");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Recordatorio)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_recordatorio_vehiculo");

            entity.HasOne(d => d.Verificacion).WithMany(p => p.Recordatorio)
                .HasForeignKey(d => d.VerificacionId)
                .HasConstraintName("FK_recordatorio_verificacion");
        });

        modelBuilder.Entity<RecordatorioCanal>(entity =>
        {
            entity.HasKey(e => new { e.RecordatorioId, e.Canal });

            entity.ToTable("recordatorio_canal", tb => tb.HasComment("Canales de notificación de cada recordatorio."));

            entity.Property(e => e.RecordatorioId).HasColumnName("recordatorio_id");
            entity.Property(e => e.Canal)
                .HasMaxLength(20)
                .HasColumnName("canal");

            entity.HasOne(d => d.Recordatorio).WithMany(p => p.RecordatorioCanal)
                .HasForeignKey(d => d.RecordatorioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_recordatorio_canal_recordatorio");
        });

        modelBuilder.Entity<ServicioMantenimiento>(entity =>
        {
            entity.ToTable("servicio_mantenimiento", tb =>
                {
                    tb.HasComment("Bitácora de servicios de mantenimiento.");
                    tb.HasTrigger("TR_servicio_mantenimiento_actualizado_en");
                });

            entity.HasIndex(e => new { e.VehiculoId, e.Fecha }, "IX_servicio_vehiculo_fecha");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.ComprobanteUrl)
                .HasMaxLength(500)
                .HasColumnName("comprobante_url");
            entity.Property(e => e.Costo)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("costo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FechaRetorno).HasColumnName("fecha_retorno");
            entity.Property(e => e.Folio)
                .HasMaxLength(50)
                .HasColumnName("folio");
            entity.Property(e => e.KilometrajeServicio).HasColumnName("kilometraje_servicio");
            entity.Property(e => e.MetaProximoKm).HasColumnName("meta_proximo_km");
            entity.Property(e => e.Notas)
                .HasMaxLength(500)
                .HasColumnName("notas");
            entity.Property(e => e.Taller)
                .HasMaxLength(120)
                .HasColumnName("taller");
            entity.Property(e => e.TipoServicioId).HasColumnName("tipo_servicio_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.TipoServicio).WithMany(p => p.ServicioMantenimiento)
                .HasForeignKey(d => d.TipoServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_servicio_tipo");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.ServicioMantenimiento)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_servicio_vehiculo");
        });

        modelBuilder.Entity<SuscripcionPush>(entity =>
        {
            entity.ToTable("suscripcion_push", tb => tb.HasComment("Suscripciones Web Push por dispositivo."));

            entity.HasIndex(e => e.UsuarioId, "IX_suscripcion_push_usuario");

            entity.HasIndex(e => e.Endpoint, "UQ_suscripcion_push_endpoint").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.ClaveAuth)
                .HasMaxLength(255)
                .HasColumnName("clave_auth");
            entity.Property(e => e.ClaveP256dh)
                .HasMaxLength(255)
                .HasColumnName("clave_p256dh");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Dispositivo)
                .HasMaxLength(100)
                .HasColumnName("dispositivo");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(500)
                .HasColumnName("endpoint");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.SuscripcionPush)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_suscripcion_push_usuario");
        });

        modelBuilder.Entity<TipoCombustible>(entity =>
        {
            entity.ToTable("tipo_combustible", tb => tb.HasComment("Catálogo de tipos de combustible."));

            entity.HasIndex(e => e.Nombre, "UQ_tipo_combustible_nombre").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TipoServicio>(entity =>
        {
            entity.ToTable("tipo_servicio", tb => tb.HasComment("Catálogo de tipos de servicio de mantenimiento."));

            entity.HasIndex(e => e.Nombre, "UQ_tipo_servicio_nombre").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuario", tb =>
                {
                    tb.HasComment("Cuenta de acceso a la aplicación.");
                    tb.HasTrigger("TR_usuario_actualizado_en");
                });

            entity.HasIndex(e => e.Correo, "UQ_usuario_correo").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(255)
                .HasColumnName("contrasena_hash");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(30)
                .HasDefaultValue("estandar")
                .HasColumnName("tipo_cuenta");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.ToTable("vehiculo", tb =>
                {
                    tb.HasComment("Vehículo del usuario; centro del expediente.");
                    tb.HasTrigger("TR_vehiculo_actualizado_en");
                });

            entity.HasIndex(e => e.UsuarioId, "IX_vehiculo_usuario");

            entity.HasIndex(e => e.UsuarioId, "UX_vehiculo_principal")
                .IsUnique()
                .HasFilter("([es_principal]=(1) AND [eliminado_en] IS NULL)");

            entity.HasIndex(e => new { e.UsuarioId, e.Placas }, "UX_vehiculo_usuario_placas")
                .IsUnique()
                .HasFilter("([eliminado_en] IS NULL)");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Anio).HasColumnName("anio");
            entity.Property(e => e.CapacidadTanqueL)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("capacidad_tanque_l");
            entity.Property(e => e.Color)
                .HasMaxLength(30)
                .HasColumnName("color");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.EsPrincipal).HasColumnName("es_principal");
            entity.Property(e => e.Estatus)
                .HasMaxLength(20)
                .HasDefaultValue("activo")
                .HasColumnName("estatus");
            entity.Property(e => e.FotoUrl)
                .HasMaxLength(500)
                .HasColumnName("foto_url");
            entity.Property(e => e.KilometrajeInicial).HasColumnName("kilometraje_inicial");
            entity.Property(e => e.Marca)
                .HasMaxLength(50)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(50)
                .HasColumnName("modelo");
            entity.Property(e => e.NotasTecnicas)
                .HasMaxLength(500)
                .HasColumnName("notas_tecnicas");
            entity.Property(e => e.OdometroActual).HasColumnName("odometro_actual");
            entity.Property(e => e.Placas)
                .HasMaxLength(15)
                .HasColumnName("placas");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Vin)
                .HasMaxLength(17)
                .HasColumnName("vin");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Vehiculo)
                .HasForeignKey<Vehiculo>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vehiculo_usuario");
        });

        modelBuilder.Entity<VerificacionVehicular>(entity =>
        {
            entity.ToTable("verificacion_vehicular", tb =>
                {
                    tb.HasComment("Historial de verificaciones ambientales.");
                    tb.HasTrigger("TR_verificacion_vehicular_actualizado_en");
                });

            entity.HasIndex(e => e.VehiculoId, "IX_verificacion_vehiculo");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Costo)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("costo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("creado_en");
            entity.Property(e => e.EliminadoEn).HasColumnName("eliminado_en");
            entity.Property(e => e.Engomado)
                .HasMaxLength(20)
                .HasColumnName("engomado");
            entity.Property(e => e.FechaVerificacion).HasColumnName("fecha_verificacion");
            entity.Property(e => e.Holograma)
                .HasMaxLength(5)
                .HasColumnName("holograma");
            entity.Property(e => e.Periodo)
                .HasMaxLength(30)
                .HasColumnName("periodo");
            entity.Property(e => e.ProximaFecha).HasColumnName("proxima_fecha");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.Verificentro)
                .HasMaxLength(100)
                .HasColumnName("verificentro");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.VerificacionVehicular)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_verificacion_vehiculo");
        });

        modelBuilder.Entity<VwGastosConsolidados>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_gastos_consolidados");

            entity.Property(e => e.Categoria)
                .HasMaxLength(50)
                .HasColumnName("categoria");
            entity.Property(e => e.Concepto)
                .HasMaxLength(120)
                .HasColumnName("concepto");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Monto)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("monto");
            entity.Property(e => e.Odometro).HasColumnName("odometro");
            entity.Property(e => e.Origen)
                .HasMaxLength(12)
                .HasColumnName("origen");
            entity.Property(e => e.OrigenId).HasColumnName("origen_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
