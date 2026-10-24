using System.ComponentModel.DataAnnotations;
using TransMetroUMG.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class BusFormulario
{
    [Range(0, int.MaxValue)]
    public int CodBus { get; set; }

    [Required(ErrorMessage = "Seleccione municipalidad.")]
    [Range(1, int.MaxValue,
        ErrorMessage = "Dato invalido.")]
    public int? CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "ingrese codigo o numero del bus.")]
    [StringLength(30,
        ErrorMessage = "maximo 30 caracteres.")]
    [Display(Name = "Número del bus")]
    public string NumeroBus { get; set; } = string.Empty;

    [Required(ErrorMessage = "placa.")]
    [StringLength(20,
        ErrorMessage = "maximo 20 caracteres.")]
    [Display(Name = "Placa")]
    public string NumeroPlaca { get; set; } = string.Empty;

    [Required(ErrorMessage = "capacidad de pasajeros.")]
    [Range(1, int.MaxValue,
    ErrorMessage = "Debe ser mayor que cero.")]
    [Display(Name = "Capacidad de pasajeros")]
    public int? CapacidadPasajeros { get; set; }

    [StringLength(100,
        ErrorMessage = "maximo de 100 caracteres.")]
    [Display(Name = "Modelo")]
    public string? ModeloBus { get; set; }

    [Display(Name = "Año")]
    public short? AnioBus { get; set; }

    [Required(ErrorMessage = "Seleccione un estado.")]
    [EnumDataType(typeof(EstadoBus),
        ErrorMessage = "dato inválido.")]
    public EstadoBus? Estado { get; set; } = EstadoBus.Activo;



}
public sealed class BusesViewModel
{
    public BusFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Bus> Registros { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Municipalidades { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}