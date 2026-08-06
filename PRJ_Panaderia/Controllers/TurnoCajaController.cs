using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;
using System.Security.Claims;

namespace PRJ_Panaderia.Controllers;

// Controlador de Turnos de Caja - Apertura, cierre y gestión de turnos de caja
[Authorize(Roles = "Admin,Cajero")]
public class TurnoCajaController : Controller
{
    private readonly TurnoCajaRepository _repository;
    private readonly EmpleadoRepository _empleadoRepo;

    public TurnoCajaController(TurnoCajaRepository repository, EmpleadoRepository empleadoRepo)
    {
        _repository = repository;
        _empleadoRepo = empleadoRepo;
    }

    // Lista turnos de caja con filtros de estado y rango de fechas
    public IActionResult Index(bool? soloAbiertos, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var turnos = _repository.Listar(soloAbiertos, fechaInicio, fechaFin);
        var turnoAbierto = _repository.ObtenerTurnoAbierto();

        ViewBag.SoloAbiertos = soloAbiertos;
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
        ViewBag.TurnoAbierto = turnoAbierto;

        return View(turnos);
    }

    // Muestra el formulario para abrir un nuevo turno de caja
    public IActionResult Create()
    {
        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claimId, out int empleadoId))
            return RedirectToAction("Index", "Login");

        var empleado = _empleadoRepo.ObtenerPorId(empleadoId);
        if (empleado == null) return NotFound();

        ViewBag.NombreEmpleado = empleado.NombreCompleto;
        return View(new TurnoCaja { IdEmpleado = empleadoId, MontoInicial = 0 });
    }

    // Valida y abre un nuevo turno de caja para un empleado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TurnoCaja turno)
    {
        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claimId, out int empleadoId))
            return RedirectToAction("Index", "Login");

        turno.IdEmpleado = empleadoId;

        if (_repository.ExisteTurnoAbiertoPorEmpleado(turno.IdEmpleado))
        {
            ModelState.AddModelError("IdEmpleado", "Este empleado ya tiene un turno abierto.");
        }

        if (!ModelState.IsValid)
        {
            var empleado = _empleadoRepo.ObtenerPorId(empleadoId);
            ViewBag.NombreEmpleado = empleado?.NombreCompleto ?? "";
            return View(turno);
        }

        _repository.Crear(turno);
        TempData["Exito"] = "Turno abierto exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar un turno abierto
    public IActionResult Edit(int id)
    {
        var turno = _repository.ObtenerPorId(id);
        if (turno == null) return NotFound();
        CargarEmpleados();
        return View(turno);
    }

    // Actualiza un turno de caja solo si está abierto
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(TurnoCaja turno)
    {
        var existente = _repository.ObtenerPorId(turno.IdTurno);
        if (existente == null) return NotFound();

        if (!existente.EstaAbierto)
        {
            TempData["Error"] = "No se puede editar un turno cerrado.";
            return RedirectToAction(nameof(Index));
        }

        if (_repository.ExisteTurnoAbiertoPorEmpleado(turno.IdEmpleado, turno.IdTurno))
        {
            ModelState.AddModelError("IdEmpleado", "Este empleado ya tiene un turno abierto.");
        }

        if (!ModelState.IsValid)
        {
            CargarEmpleados();
            return View(turno);
        }

        _repository.Actualizar(turno);
        TempData["Exito"] = "Turno actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Cierra un turno de caja con el monto de cierre y observaciones
    [HttpPost]
    public IActionResult CerrarTurno(int id, decimal montoCierre, string? observaciones)
    {
        var turno = _repository.ObtenerPorId(id);
        if (turno == null)
            return Json(new { success = false, message = "Turno no encontrado." });

        if (!turno.EstaAbierto)
            return Json(new { success = false, message = "El turno ya esta cerrado." });

        _repository.CerrarTurno(id, montoCierre, observaciones);
        return Json(new { success = true, message = "Turno cerrado exitosamente." });
    }

    // Retorna los datos de un turno de caja en formato JSON
    [HttpGet]
    public IActionResult ObtenerPorId(int id)
    {
        var turno = _repository.ObtenerPorId(id);
        if (turno == null)
            return Json(new { success = false });

        return Json(new
        {
            success = true,
            turno = new
            {
                turno.IdTurno,
                turno.NombreEmpleado,
                FechaApertura = turno.FechaApertura.ToString("dd/MM/yyyy HH:mm"),
                FechaCierre = turno.FechaCierre?.ToString("dd/MM/yyyy HH:mm"),
                turno.MontoInicial,
                turno.MontoCierre,
                turno.Observaciones,
                turno.EstaAbierto
            }
        });
    }

    // Elimina un turno de caja solo si está abierto y no tiene pedidos
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var turno = _repository.ObtenerPorId(id);
        if (turno == null)
            return Json(new { success = false, message = "Turno no encontrado." });

        if (!turno.EstaAbierto)
            return Json(new { success = false, message = "No se puede eliminar un turno cerrado." });

        var pedidosCount = _repository.ContarPedidos(id);
        if (pedidosCount > 0)
            return Json(new { success = false, message = $"No se puede eliminar el turno porque tiene {pedidosCount} pedido(s) asociado(s)." });

        _repository.Eliminar(id);
        return Json(new { success = true, message = "Turno eliminado exitosamente." });
    }

    // Carga la lista de empleados activos en ViewBag
    private void CargarEmpleados()
    {
        var empleados = _repository.ObtenerEmpleadosActivos();
        ViewBag.Empleados = new SelectList(empleados, "IdEmpleado", "NombreCompleto");
    }
}