using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class EstacionFormulario : IValidatableObject
{
    [Range(0, int.MaxValue)]
    public int CodEstacion { get; set; }

    [Required(ErrorMessage = "Selecciona una municipalidad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una municipalidad válida.")]
    public int? CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de la estación.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    [Display(Name = "Nombre de la estación")]
    public string NombreEstacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la dirección.")]
    [StringLength(250, ErrorMessage = "La dirección admite hasta 250 caracteres.")]
    [Display(Name = "Dirección")]
    public string DireccionEstacion { get; set; } = string.Empty;

    [Range(typeof(decimal), "-90", "90", ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitud { get; set; }

    [Range(typeof(decimal), "-180", "180", ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitud { get; set; }

    [Display(Name = "Capacidad máxima de pasajeros")]
    [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser un entero mayor que cero.")]
    public int? CapacidadPasajeros { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [EnumDataType(typeof(EstadoEstacion), ErrorMessage = "Selecciona un estado válido.")]
    public EstadoEstacion? Estado { get; set; } = EstadoEstacion.Activo;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitud.HasValue && decimal.Round(Latitud.Value, 7) != Latitud.Value)
            yield return new ValidationResult("La latitud admite hasta 7 decimales.", [nameof(Latitud)]);
        if (Longitud.HasValue && decimal.Round(Longitud.Value, 7) != Longitud.Value)
            yield return new ValidationResult("La longitud admite hasta 7 decimales.", [nameof(Longitud)]);
    }
}

public sealed class EstacionesViewModel
{
    public EstacionFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Estacion> Registros { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Municipalidades { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}

