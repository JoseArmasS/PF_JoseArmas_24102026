using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class ConsultasAsignacionesViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una municipalidad válida.")] public int? Municipalidad { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un piloto válido.")] public int? Piloto { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un bus válido.")] public int? Bus { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una estación válida.")] public int? Estacion { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "SeleSeleccionecciona un parqueo válido.")] public int? Parqueo { get; set; }
    [DataType(DataType.Date)] public DateOnly? Fecha { get; set; }
    [Range(1, 1000000, ErrorMessage = "La página no es válida.")] public int Pagina { get; set; } = 1;
    [BindNever] public CatalogosConsultaBuses Catalogos { get; set; } = new([], [], [], [], []);
    [BindNever] public IReadOnlyList<OpcionCatalogo> Parqueos { get; set; } = [];
    [BindNever] public PaginaConsulta<PilotoBusConsulta> PilotosBuses { get; set; } = new([], 0, 1);
    [BindNever] public PaginaConsulta<EstacionParqueoBusConsulta> EstacionesParqueos { get; set; } = new([], 0, 1);
    [BindNever] public bool ConsultaParqueos { get; set; }
    [BindNever] public bool ErrorCarga { get; set; }
}
