using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class MunicipalidadFormulario
{
    [Range(0, int.MaxValue)]
    public int CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de la municipalidad.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    [Display(Name = "Nombre de la municipalidad")]
    public string NombreMunicipalidad { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La dirección admite hasta 250 caracteres.")]
    [Display(Name = "Dirección")]
    public string? DireccionMunicipalidad { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono admite hasta 20 caracteres.")]
    [Display(Name = "Teléfono")]
    public string? TelefonoMunicipalidad { get; set; }

    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo admite hasta 150 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string? CorreoElectronico { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [EnumDataType(typeof(EstadoMunicipalidad), ErrorMessage = "Selecciona un estado válido.")]
    public EstadoMunicipalidad? Estado { get; set; } = EstadoMunicipalidad.Activo;
}

public sealed class MunicipalidadesViewModel
{
    public MunicipalidadFormulario Formulario { get; set; } = new();
    [StringLength(150)]
    public string? Buscar { get; set; }
    public int Pagina { get; set; } = 1;
    [BindNever]
    public IReadOnlyList<Municipalidad> Registros { get; set; } = [];
    [BindNever]
    public int Total { get; set; }
    [BindNever]
    public bool ErrorCarga { get; set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)Application.IMunicipalidadesService.TamanoPagina));
}
