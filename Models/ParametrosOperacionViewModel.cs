using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class ParametroFormulario : IValidatableObject
{
    [Range(0, int.MaxValue)]
    public int CodParametroOperacion { get; set; }

    [Required(ErrorMessage = "Ingresa la clave.")]
    [StringLength(100, ErrorMessage = "Espacio maximo 100 caracteres.")]
    public string Clave { get; set; } = string.Empty;

    [Range(typeof(decimal), "-99999999.9999", "99999999.9999",
        ErrorMessage = "maximo de 8 enteros y 4 decimales.")]
    public decimal? Valor { get; set; }

    [Required(ErrorMessage = "Ingresa la descripción.")]
    [StringLength(250, ErrorMessage = "epacio maximo 250 caracteres.")]
    [Display(Name = "Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Valor.HasValue && decimal.Round(Valor.Value, 4) != Valor.Value)
            yield return new ValidationResult("maximo 4 decimales.", [nameof(Valor)]);
    }
}

public sealed class ParametrosOperacionViewModel
{
    public ParametroFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Parametros> Registros { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}

