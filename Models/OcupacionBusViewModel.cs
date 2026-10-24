using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class OcupacionBusViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una operación válida.")]
    public int Operacion { get; set; }

    [Required(ErrorMessage = "Selecciona la estación de la medición.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una estación válida.")]
    [Display(Name = "Estación de la medición")]
    public int? Estacion { get; set; }

    [Required(ErrorMessage = "Ingresa la cantidad de pasajeros.")]
    [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser un entero mayor o igual a cero.")]
    [Display(Name = "Pasajeros dentro del bus")]
    public int? Pasajeros { get; set; }

    [BindNever] public OperacionBusConsulta? Datos { get; set; }
    [BindNever] public IReadOnlyList<EstacionSalidaOpcion> Recorrido { get; set; } = [];
    [BindNever] public IReadOnlyList<MedicionBusConsulta> Historial { get; set; } = [];
    [BindNever] public MedicionBusConsulta? Ultima { get; set; }
    [BindNever] public int Pagina { get; set; } = 1;
    [BindNever] public bool HaySiguiente { get; set; }
    [BindNever] public bool ErrorCarga { get; set; }
    [BindNever] public bool SoloConsulta { get; set; }
    [BindNever] public bool SolicitarMasBuses { get; set; }
}
