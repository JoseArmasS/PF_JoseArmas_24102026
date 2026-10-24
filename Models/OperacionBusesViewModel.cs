using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class OperacionBusesViewModel
{
    [BindNever]
    public DateTime? HoraReferencia { get; set; }

    [Display(Name = "Fecha de asignación")]
    [DataType(DataType.Date)]
    public DateOnly? Fecha { get; set; }

    [Display(Name = "Línea")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una línea válida.")]
    public int? CodigoLinea { get; set; }

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Lineas { get; set; } = [];

    [BindNever]
    public IReadOnlyList<AsignacionOperacionConsulta> Registros { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }

    [BindNever]
    public IReadOnlyList<OperacionBusConsulta> Operaciones { get; set; } = [];

    [BindNever]
    public bool ErrorOperaciones { get; set; }

    [BindNever]
    public IReadOnlyList<EstacionOperacionOpcion> RecorridosBuses { get; set; } = [];

    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [BindNever]
    public OcupacionEstacionViewModel Ocupacion { get; set; } = new();
}
