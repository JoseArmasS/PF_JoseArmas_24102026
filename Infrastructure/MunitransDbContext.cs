using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

/// <summary>
/// Conexión a la base existente. 
/// </summary>
public sealed class MunitransDbContext(DbContextOptions<MunitransDbContext> options)
    : DbContext(options)
{
    public DbSet<Municipalidad> Municipalidades => Set<Municipalidad>();
    public DbSet <Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Parametros> ParametrosOperacion => Set<Parametros>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Piloto> Pilotos => Set<Piloto>();
    public DbSet<AsignacionBus> AsignacionesBuses => Set<AsignacionBus>();
    public DbSet<Estacion> Estaciones => Set<Estacion>();
    public DbSet<Parqueo> Parqueos => Set<Parqueo>();
    public DbSet<Linea> Lineas => Set<Linea>();
    public DbSet<LineaEstacion> LineasEstaciones => Set<LineaEstacion>();
    public DbSet<OperacionBus> OperacionesBuses => Set<OperacionBus>();
    public DbSet<ParadaOperacionBus> ParadasOperacionesBuses => Set<ParadaOperacionBus>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        MapeoOperacionBus.Configurar(modelBuilder);
        var parqueo = modelBuilder.Entity<Parqueo>();
        parqueo.ToTable("PARQUEO", "dbo", tabla =>
        {
            tabla.HasCheckConstraint("CK_PARQUEO_CAPACIDAD", "[CAPACIDAD_TOTAL] > 0");
            tabla.HasCheckConstraint("CK_PARQUEO_ESTADO", "[COD_ESTADO_PARQUEO] IN (0, 1)");
        });
        parqueo.HasKey(x => x.CodParqueo).HasName("PK_PARQUEO");
        parqueo.Property(x => x.CodParqueo).HasColumnName("COD_PARQUEO").UseIdentityColumn();
        parqueo.Property(x => x.CodEstacion).HasColumnName("COD_ESTACION").IsRequired();
        parqueo.Property(x => x.NombreParqueo).HasColumnName("NOMBRE_PARQUEO")
            .HasMaxLength(150).IsUnicode(false).IsRequired();
        parqueo.Property(x => x.DireccionParqueo).HasColumnName("DIRECCION_PARQUEO")
            .HasMaxLength(250).IsUnicode(false).IsRequired();
        parqueo.Property(x => x.CapacidadTotal).HasColumnName("CAPACIDAD_TOTAL").IsRequired();
        parqueo.Property(x => x.Estado).HasColumnName("COD_ESTADO_PARQUEO")
            .HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        parqueo.HasOne<Estacion>().WithMany().HasForeignKey(x => x.CodEstacion)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PARQUEO_ESTACION");

        var asignacion = modelBuilder.Entity<AsignacionBus>();
        asignacion.ToTable("ASIGNACION_BUS", "dbo");
        asignacion.HasKey(x => x.CodAsignacionBus).HasName("PK_ASIGNACION_BUS");
        asignacion.Property(x => x.CodAsignacionBus).HasColumnName("COD_ASIGNACION_BUS").UseIdentityColumn();
        asignacion.Property(x => x.FechaAsignacion).HasColumnName("FECHA_ASIGNACION").HasColumnType("date").IsRequired();
        asignacion.Property(x => x.CodBus).HasColumnName("COD_BUS").IsRequired();
        asignacion.Property(x => x.CodPiloto).HasColumnName("COD_PILOTO").IsRequired();
        asignacion.Property(x => x.CodLinea).HasColumnName("COD_LINEA").IsRequired();
        asignacion.HasOne<Bus>().WithMany().HasForeignKey(x => x.CodBus)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ASIGNACION_BUS_BUS");
        asignacion.HasOne<Piloto>().WithMany().HasForeignKey(x => x.CodPiloto)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ASIGNACION_BUS_PILOTO");
        asignacion.HasOne<Linea>().WithMany().HasForeignKey(x => x.CodLinea)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ASIGNACION_BUS_LINEA");

        var piloto = modelBuilder.Entity<Piloto>();
        piloto.Property(x => x.TelefonoPiloto).HasColumnName("TELEFONO_PILOTO")
            .HasMaxLength(25).IsUnicode(false);
        piloto.Property(x => x.HistorialEducativo).HasColumnName("HISTORIAL_EDUCATIVO")
            .HasMaxLength(2000).IsUnicode();
        piloto.ToTable("PILOTO", "dbo");
        piloto.HasKey(x => x.CodPiloto).HasName("PK_PILOTO");
        piloto.Property(x => x.CodPiloto).HasColumnName("COD_PILOTO").UseIdentityColumn();
        piloto.Property(x => x.CodMunicipalidad).HasColumnName("COD_MUNICIPALIDAD").IsRequired();
        piloto.HasOne<Municipalidad>().WithMany().HasForeignKey(x => x.CodMunicipalidad)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PILOTO_MUNICIPALIDAD");
        piloto.Property(x => x.NombresPiloto).HasColumnName("NOMBRES_PILOTO")
            .HasMaxLength(100).IsUnicode(false).IsRequired();
        piloto.Property(x => x.ApellidosPiloto).HasColumnName("APELLIDOS_PILOTO")
            .HasMaxLength(100).IsUnicode(false).IsRequired();
        piloto.Property(x => x.NumeroLicencia).HasColumnName("NUMERO_LICENCIA")
            .HasMaxLength(50).IsUnicode(false).IsRequired();
        piloto.HasIndex(x => x.NumeroLicencia).IsUnique().HasDatabaseName("UQ_PILOTO_LICENCIA");
        piloto.Property(x => x.TipoLicencia).HasColumnName("TIPO_LICENCIA")
            .HasMaxLength(20).IsUnicode(false).IsRequired();
        piloto.Property(x => x.FechaVencimientoLicencia).HasColumnName("FECHA_VENCIMIENTO_LICENCIA")
            .HasColumnType("date").IsRequired();
        piloto.Property(x => x.Estado).HasColumnName("COD_ESTADO_PILOTO")
            .HasConversion<byte>().IsRequired();

        var recorrido = modelBuilder.Entity<LineaEstacion>();
        recorrido.ToTable("LINEA_ESTACION", "dbo");
        recorrido.HasKey(x => x.CodLineaEstacion);
        recorrido.Property(x => x.CodLineaEstacion).HasColumnName("COD_LINEA_ESTACION").UseIdentityColumn();
        recorrido.Property(x => x.CodLinea).HasColumnName("COD_LINEA").IsRequired();
        recorrido.Property(x => x.CodEstacion).HasColumnName("COD_ESTACION").IsRequired();
        recorrido.Property(x => x.OrdenEstacion).HasColumnName("ORDEN_ESTACION").IsRequired();
        recorrido.Property(x => x.DistanciaSiguienteKm).HasColumnName("DISTANCIA_SIGUIENTE_KM").HasPrecision(8, 3);
        recorrido.HasOne<Linea>().WithMany().HasForeignKey(x => x.CodLinea)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LINEA_ESTACION_LINEA");
        recorrido.HasOne<Estacion>().WithMany().HasForeignKey(x => x.CodEstacion)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LINEA_ESTACION_ESTACION");
        recorrido.HasIndex(x => new { x.CodLinea, x.CodEstacion })
            .IsUnique().HasDatabaseName("UQ_LINEA_ESTACION");
        recorrido.HasIndex(x => new { x.CodLinea, x.OrdenEstacion })
            .IsUnique().HasDatabaseName("UQ_LINEA_ORDEN");

        var linea = modelBuilder.Entity<Linea>();
        linea.ToTable("LINEA", "dbo");
        linea.HasKey(x => x.CodLinea);
        linea.Property(x => x.CodLinea).HasColumnName("COD_LINEA").UseIdentityColumn();
        linea.Property(x => x.CodMunicipalidad).HasColumnName("COD_MUNICIPALIDAD").IsRequired();
        linea.HasOne<Municipalidad>().WithMany().HasForeignKey(x => x.CodMunicipalidad)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LINEA_MUNICIPALIDAD");
        linea.Property(x => x.NombreLinea).HasColumnName("NOMBRE_LINEA")
            .HasMaxLength(100).IsUnicode(false).IsRequired();
        linea.Property(x => x.DescripcionLinea).HasColumnName("DESCRIPCION_LINEA")
            .HasMaxLength(250).IsUnicode(false);
        linea.Property(x => x.Estado).HasColumnName("COD_ESTADO_LINEA")
            .HasConversion<byte>().IsRequired();

        var estacion = modelBuilder.Entity<Estacion>();
        estacion.ToTable("ESTACION", "dbo");
        estacion.HasKey(x => x.CodEstacion);
        estacion.Property(x => x.CodEstacion).HasColumnName("COD_ESTACION").UseIdentityColumn();
        estacion.Property(x => x.CodMunicipalidad).HasColumnName("COD_MUNICIPALIDAD").IsRequired();
        estacion.HasOne<Municipalidad>().WithMany().HasForeignKey(x => x.CodMunicipalidad)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ESTACION_MUNICIPALIDAD");
        estacion.Property(x => x.NombreEstacion).HasColumnName("NOMBRE_ESTACION")
            .HasMaxLength(150).IsUnicode(false).IsRequired();
        estacion.Property(x => x.DireccionEstacion).HasColumnName("DIRECCION_ESTACION")
            .HasMaxLength(250).IsUnicode(false).IsRequired();
        estacion.Property(x => x.Latitud).HasColumnName("LATITUD").HasPrecision(10, 7);
        estacion.Property(x => x.Longitud).HasColumnName("LONGITUD").HasPrecision(10, 7);
        estacion.Property(x => x.CapacidadPasajeros).HasColumnName("CAPACIDAD_PASAJEROS");
        estacion.Property(x => x.Estado).HasColumnName("COD_ESTADO_ESTACION")
            .HasConversion<byte>().IsRequired();

        var parametro = modelBuilder.Entity<Parametros>();
        parametro.ToTable("PARAMETRO_OPERACION", "dbo");
        parametro.HasKey(x => x.CodParametroOperacion);
        parametro.Property(x => x.CodParametroOperacion)
            .HasColumnName("COD_PARAMETRO_OPERACION")
            .UseIdentityColumn();
        parametro.Property(x => x.Clave)
            .HasColumnName("CLAVE")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        parametro.HasIndex(x => x.Clave)
            .IsUnique()
            .HasDatabaseName("UQ_PARAMETRO_OPERACION_CLAVE");
        parametro.Property(x => x.Valor)
            .HasColumnName("VALOR")
            .HasPrecision(12, 4);
        parametro.Property(x => x.Descripcion)
            .HasColumnName("DESCRIPCION")
            .HasMaxLength(250)
            .IsUnicode(false)
            .IsRequired();


        /*  bus */
        var bus = modelBuilder.Entity<Bus>();
        bus.Property(x => x.CodParqueoBase).HasColumnName("COD_PARQUEO_BASE");
        bus.HasOne<Parqueo>().WithMany().HasForeignKey(x => x.CodParqueoBase)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_BUS_PARQUEO_BASE");
        bus.HasIndex(x => x.CodParqueoBase).HasDatabaseName("IX_BUS_PARQUEO_BASE");

        bus.ToTable("BUS", "dbo");

        bus.HasKey(x => x.CodBus);

        bus.Property(x => x.CodBus)
            .HasColumnName("COD_BUS")
            .UseIdentityColumn();
        bus.Property(x => x.CodMunicipalidad)
            .HasColumnName("COD_MUNICIPALIDAD")
            .IsRequired();

        bus.HasOne<Municipalidad>()
            .WithMany()
            .HasForeignKey(x => x.CodMunicipalidad)
            .OnDelete(DeleteBehavior.NoAction);

        bus.Property(x => x.NumeroBus)
            .HasColumnName("NUMERO_BUS")
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        bus.Property(x => x.NumeroPlaca)
            .HasColumnName("NUMERO_PLACA")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        bus.HasIndex(x => x.NumeroPlaca)
            .IsUnique()
            .HasDatabaseName("UQ_BUS_PLACA");

        bus.Property(x => x.CapacidadPasajeros)
            .HasColumnName("CAPACIDAD_PASAJEROS")
            .IsRequired();

        bus.Property(x => x.ModeloBus)
            .HasColumnName("MODELO_BUS")
            .HasMaxLength(100)
            .IsUnicode(false);

        bus.Property(x => x.AnioBus)
            .HasColumnName("ANIO_BUS");

        bus.Property(x => x.Estado)
            .HasColumnName("COD_ESTADO_BUS")
            .HasConversion<byte>()
            .IsRequired();



        /* muni */
        var entidad = modelBuilder.Entity<Municipalidad>();
        entidad.ToTable("MUNICIPALIDAD", "dbo");
        entidad.HasKey(x => x.CodMunicipalidad);
        
        entidad.Property(x => x.CodMunicipalidad)
            .HasColumnName("COD_MUNICIPALIDAD")
            .UseIdentityColumn();

        entidad.Property(x => x.NombreMunicipalidad)
            .HasColumnName("NOMBRE_MUNICIPALIDAD")
            .HasMaxLength(150)
            .IsUnicode(false)
            .IsRequired();

        entidad.Property(x => x.DireccionMunicipalidad)
            .HasColumnName("DIRECCION_MUNICIPALIDAD")
            .HasMaxLength(250).IsUnicode(false);
        
        entidad.Property(x => x.TelefonoMunicipalidad)
            .HasColumnName("TELEFONO_MUNICIPALIDAD")
            .HasMaxLength(20)
            .IsUnicode(false);
        
        entidad.Property(x => x.CorreoElectronico)
            .HasColumnName("CORREO_ELECTRONICO")
            .HasMaxLength(150)
            .IsUnicode(false);
        
        entidad.Property(x => x.Estado)
            .HasColumnName("COD_ESTADO_MUNICIPALIDAD")
            .HasConversion<byte>()
            .HasColumnType("tinyint");
    
        var rol = modelBuilder.Entity<Rol>();
        rol.ToTable("ROL", "dbo");
        rol.HasKey(x => x.CodRol);
        
        rol.Property(x => x.CodRol)
            .HasColumnName("COD_ROL")
            .UseIdentityColumn();
        
        rol.Property(x => x.NombreRol)
            .HasColumnName("NOMBRE_ROL")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        
        rol.HasIndex(x => x.NombreRol)
            .IsUnique()
            .HasDatabaseName("UQ_ROL_NOMBRE");
        
        rol.Property(x => x.DescripcionRol)
            .HasColumnName("DESCRIPCION_ROL")
            .HasMaxLength(250)
            .IsUnicode(false);
        
        rol.Property(x => x.Estado)
            .HasColumnName("COD_ESTADO_ROL")
            .HasConversion<byte>()
            .HasColumnType("tinyint");

        var usuario = modelBuilder.Entity<Usuario>();

        usuario.ToTable("USUARIO", "dbo");
        usuario.HasKey(x => x.CodUsuario);

        usuario.Property(x => x.CodUsuario)
            .HasColumnName("COD_USUARIO")
            .UseIdentityColumn();

        usuario.Property(x => x.CodMunicipalidad)
            .HasColumnName("COD_MUNICIPALIDAD");

        usuario.Property(x => x.CodRol)
            .HasColumnName("COD_ROL");

        usuario.Property(x => x.NombreUsuario)
            .HasColumnName("NOMBRE_USUARIO")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        usuario.HasIndex(x => x.NombreUsuario)
            .IsUnique()
            .HasDatabaseName("UQ_USUARIO_NOMBRE");

        usuario.Property(x => x.NombreCompleto)
            .HasColumnName("NOMBRE_COMPLETO")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        usuario.Property(x => x.CorreoElectronico)
            .HasColumnName("CORREO_ELECTRONICO")
            .HasMaxLength(150)
            .IsUnicode(false)
            .IsRequired();

        usuario.Property(x => x.ContrasenaHash)
            .HasColumnName("CONTRASENA")
            .HasMaxLength(255)
            .IsUnicode(false)
            .IsRequired();

        usuario.Property(x => x.FechaExpiracionContrasena)
            .HasColumnName("FECHA_EXPIRACION_CONTRASENA")
            .HasColumnType("datetime2(7)");

        usuario.Property(x => x.Estado)
            .HasColumnName("COD_ESTADO_USUARIO")
            .HasConversion<byte>()
            .HasColumnType("tinyint");

        usuario.Property(x => x.FechaCreacion)
            .HasColumnName("FECHA_CREACION")
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("sysdatetime()");

        usuario.Property(x => x.UltimoAcceso)
            .HasColumnName("ULTIMO_ACCESO")
            .HasColumnType("datetime2(7)");

        usuario.HasOne<Municipalidad>()
            .WithMany()
            .HasForeignKey(x => x.CodMunicipalidad)
            .OnDelete(DeleteBehavior.NoAction);

        usuario.HasOne<Rol>()
            .WithMany()
            .HasForeignKey(x => x.CodRol)
            .OnDelete(DeleteBehavior.NoAction);

    }




}
