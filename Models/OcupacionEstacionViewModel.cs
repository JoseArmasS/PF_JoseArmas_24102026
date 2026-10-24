using System.ComponentModel.DataAnnotations;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class MedicionEstacionFormulario
{
    [Required(ErrorMessage = "Selecciona una estación.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una estación válida.")]
    public int? CodEstacion { get; set; }

    [Required(ErrorMessage = "Ingresa la cantidad estimada de pasajeros.")]
    [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser un entero mayor o igual a cero.")]
    [Display(Name = "Pasajeros estimados en la estación")]
    public int? Pasajeros { get; set; }
}

public sealed class OcupacionEstacionViewModel
{
    public int? CodEstacion { get; set; }
    public int Pagina { get; set; } = 1;
    public IReadOnlyList<EstacionOcupacionConsulta> Estaciones { get; set; } = [];
    public EstacionOcupacionConsulta? Seleccionada { get; set; }
    public IReadOnlyList<MedicionEstacionConsulta> Historial { get; set; } = [];
    public MedicionEstacionConsulta? Ultima { get; set; }
    public MedicionEstacionFormulario Formulario { get; set; } = new();
    public bool HaySiguiente { get; set; }
    public bool ErrorCarga { get; set; }
}
