using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

internal static class MapeoOperacionBus
{
    public static void Configurar(ModelBuilder modelo)
    {
        var operacion = modelo.Entity<OperacionBus>();
        operacion.ToTable("OPERACION_BUS", "dbo");
        operacion.HasKey(x => x.CodOperacionBus);
        operacion.Property(x => x.CodOperacionBus).HasColumnName("COD_OPERACION_BUS").UseIdentityColumn();
        operacion.Property(x => x.CodAsignacionBus).HasColumnName("COD_ASIGNACION_BUS");
        operacion.Property(x => x.CodEstacionSalida).HasColumnName("COD_ESTACION_SALIDA");
        operacion.Property(x => x.CodUsuarioOperador).HasColumnName("COD_USUARIO_OPERADOR");
        operacion.Property(x => x.FechaHoraRegistro).HasColumnName("FECHA_HORA_REGISTRO").HasColumnType("datetime2(7)");
        operacion.Property(x => x.FechaHoraInicioEspera).HasColumnName("FECHA_HORA_INICIO_ESPERA").HasColumnType("datetime2(7)");
        operacion.Property(x => x.FechaHoraSalida).HasColumnName("FECHA_HORA_SALIDA").HasColumnType("datetime2(7)");
        operacion.Property(x => x.CapacidadRegistrada).HasColumnName("CAPACIDAD_REGISTRADA");
        operacion.Property(x => x.SentidoInicial).HasColumnName("SENTIDO_INICIAL");
        operacion.Property(x => x.FechaHoraCierre).HasColumnName("FECHA_HORA_CIERRE").HasColumnType("datetime2(7)");
        operacion.Property(x => x.CodUsuarioCierre).HasColumnName("COD_USUARIO_CIERRE");
        operacion.HasOne<AsignacionBus>().WithMany().HasForeignKey(x => x.CodAsignacionBus).OnDelete(DeleteBehavior.NoAction);
        operacion.HasOne<Estacion>().WithMany().HasForeignKey(x => x.CodEstacionSalida).OnDelete(DeleteBehavior.NoAction);
        operacion.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CodUsuarioOperador).OnDelete(DeleteBehavior.NoAction);
        operacion.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CodUsuarioCierre).OnDelete(DeleteBehavior.NoAction);

        var parada = modelo.Entity<ParadaOperacionBus>();
        parada.ToTable("OPERACION_BUS_PARADA", "dbo");
        parada.HasKey(x => x.CodOperacionBusParada);
        parada.Property(x => x.CodOperacionBusParada).HasColumnName("COD_OPERACION_BUS_PARADA").UseIdentityColumn();
        parada.Property(x => x.CodOperacionBus).HasColumnName("COD_OPERACION_BUS");
        parada.Property(x => x.Secuencia).HasColumnName("SECUENCIA");
        parada.Property(x => x.CodEstacion).HasColumnName("COD_ESTACION");
        parada.Property(x => x.FechaHoraLlegada).HasColumnName("FECHA_HORA_LLEGADA").HasColumnType("datetime2(7)");
        parada.Property(x => x.CodUsuarioLlegada).HasColumnName("COD_USUARIO_LLEGADA");
        parada.Property(x => x.FechaHoraSalida).HasColumnName("FECHA_HORA_SALIDA").HasColumnType("datetime2(7)");
        parada.Property(x => x.CodUsuarioSalida).HasColumnName("COD_USUARIO_SALIDA");
        parada.HasIndex(x => new { x.CodOperacionBus, x.Secuencia }).IsUnique().HasDatabaseName("UQ_OPERACION_BUS_PARADA_SECUENCIA");
        parada.HasOne<OperacionBus>().WithMany().HasForeignKey(x => x.CodOperacionBus).OnDelete(DeleteBehavior.NoAction);
        parada.HasOne<Estacion>().WithMany().HasForeignKey(x => x.CodEstacion).OnDelete(DeleteBehavior.NoAction);
        parada.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CodUsuarioLlegada).OnDelete(DeleteBehavior.NoAction);
        parada.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CodUsuarioSalida).OnDelete(DeleteBehavior.NoAction);
    }
}
