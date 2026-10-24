using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Domain;
using TransMetroUMG.Infrastructure;
using TransMetroUMG.Application;
using TransMetroUMG.Models;
using Microsoft.Data.SqlClient;

namespace TransMetroUMG.Controllers
{
    public class ParametrosOperacionController : Controller
    {
        private readonly MunitransDbContext _context;
        private readonly IParametrosOperacionService _servicio;
        private readonly ILogger<ParametrosOperacionController> _logger;

        public ParametrosOperacionController(MunitransDbContext context,
            IParametrosOperacionService servicio, ILogger<ParametrosOperacionController> logger)
        {
            _context = context;
            _servicio = servicio;
            _logger = logger;
        }

        // GET: ParametrosOperacion
        public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid || id <= 0) return BadRequest();
            var modelo = new ParametrosOperacionViewModel();
            try
            {
                if (id.HasValue)
                {
                    var registro = await _servicio.ObtenerAsync(id.Value, cancellationToken);
                    if (registro is null) return NotFound();
                    modelo.Formulario = new ParametroFormulario
                    {
                        CodParametroOperacion = registro.CodParametroOperacion,
                        Clave = registro.Clave,
                        Valor = registro.Valor,
                        Descripcion = registro.Descripcion
                    };
                }
            }
            catch (SqlException exception) { InformarError(exception, "consultar", modelo); }
            await CargarTablaAsync(modelo, cancellationToken);
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(ParametrosOperacionViewModel modelo,
            CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var formulario = modelo.Formulario;
                    var resultado = await _servicio.GuardarAsync(new Parametros
                    {
                        CodParametroOperacion = formulario.CodParametroOperacion,
                        Clave = formulario.Clave,
                        Valor = formulario.Valor,
                        Descripcion = formulario.Descripcion
                    }, cancellationToken);
                    if (resultado == ResultadoGuardarParametro.NoEncontrado) return NotFound();
                    if (resultado == ResultadoGuardarParametro.Guardado)
                    {
                        TempData["ParametroGuardado"] = formulario.CodParametroOperacion == 0
                            ? "Parámetro registrado correctamente." : "Parámetro actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError("Formulario.Clave", "Ya existe un parámetro con esa clave.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    _logger.LogWarning("El parámetro {Codigo} ya no estaba disponible al guardar.",
                        modelo.Formulario.CodParametroOperacion);
                    ModelState.AddModelError(string.Empty, "El registro ya no está disponible. Recarga la pantalla.");
                }
                catch (DbUpdateException exception) { InformarError(exception, "guardar", modelo); }
                catch (SqlException exception) { InformarError(exception, "guardar", modelo); }
            }
            await CargarTablaAsync(modelo, cancellationToken);
            return View("Index", modelo);
        }

        private async Task CargarTablaAsync(ParametrosOperacionViewModel modelo, CancellationToken cancellationToken)
        {
            try { modelo.Registros = await _servicio.ConsultarAsync(cancellationToken); }
            catch (SqlException exception) { InformarError(exception, "consultar", modelo); }
        }

        private void InformarError(Exception exception, string operacion, ParametrosOperacionViewModel modelo)
        {
            var sql = exception as SqlException ?? exception.InnerException as SqlException;
            _logger.LogError("Error al {Operacion} parámetros. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
                operacion, exception.GetType().Name, sql?.Number, sql?.State);
            if (operacion == "consultar") modelo.ErrorCarga = true;
            ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
        }

        // GET: ParametrosOperacion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parametros = await _context.ParametrosOperacion
                .FirstOrDefaultAsync(m => m.CodParametroOperacion == id);
            if (parametros == null)
            {
                return NotFound();
            }

            return View(parametros);
        }

        // GET: ParametrosOperacion/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ParametrosOperacion/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CodParametroOperacion,Clave,Valor,Descripcion")] Parametros parametros)
        {
            if (ModelState.IsValid)
            {
                _context.Add(parametros);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(parametros);
        }

        // GET: ParametrosOperacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parametros = await _context.ParametrosOperacion.FindAsync(id);
            if (parametros == null)
            {
                return NotFound();
            }
            return View(parametros);
        }

        // POST: ParametrosOperacion/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CodParametroOperacion,Clave,Valor,Descripcion")] Parametros parametros)
        {
            if (id != parametros.CodParametroOperacion)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(parametros);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParametrosExists(parametros.CodParametroOperacion))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(parametros);
        }

        // GET: ParametrosOperacion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parametros = await _context.ParametrosOperacion
                .FirstOrDefaultAsync(m => m.CodParametroOperacion == id);
            if (parametros == null)
            {
                return NotFound();
            }

            return View(parametros);
        }

        // POST: ParametrosOperacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var parametros = await _context.ParametrosOperacion.FindAsync(id);
            if (parametros != null)
            {
                _context.ParametrosOperacion.Remove(parametros);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ParametrosExists(int id)
        {
            return _context.ParametrosOperacion.Any(e => e.CodParametroOperacion == id);
        }
    }
}
