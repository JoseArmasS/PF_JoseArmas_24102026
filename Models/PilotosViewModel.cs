using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class PilotoFormulario
{
    [Range(0, int.MaxValue)]
    public int CodPiloto { get; set; }

    [Required(ErrorMessage = "Selecciona una municipalidad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una municipalidad válida.")]
    public int? CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "Ingresa los nombres del piloto.")]
    [StringLength(100, ErrorMessage = "Los nombres admiten hasta 100 caracteres.")]
    public string NombresPiloto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa los apellidos del piloto.")]
    [StringLength(100, ErrorMessage = "Los apellidos admiten hasta 100 caracteres.")]
    public string ApellidosPiloto { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "El teléfono admite hasta 25 caracteres.")]
    [Phone(ErrorMessage = "Ingresa un número de teléfono válido.")]
    public string? TelefonoPiloto { get; set; }

    [StringLength(2000, ErrorMessage = "El historial educativo admite hasta 2000 caracteres.")]
    public string? HistorialEducativo { get; set; }

    [Required(ErrorMessage = "Ingresa el número de licencia.")]
    [StringLength(50, ErrorMessage = "El número de licencia admite hasta 50 caracteres.")]
    public string NumeroLicencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el tipo de licencia.")]
    [StringLength(20, ErrorMessage = "El tipo de licencia admite hasta 20 caracteres.")]
    public string TipoLicencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la fecha de vencimiento de la licencia.")]
    [DataType(DataType.Date)]
    public DateOnly? FechaVencimientoLicencia { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [EnumDataType(typeof(EstadoPiloto), ErrorMessage = "Selecciona un estado válido.")]
    public EstadoPiloto? Estado { get; set; } = EstadoPiloto.Activo;
}

public sealed class PilotosViewModel
{
    public PilotoFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Piloto> Registros { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Municipalidades { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}
