using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;

namespace PRJ_Panaderia.Controllers;

// Controlador de Auditoría - Consulta y reportes de registros de auditoría
[Authorize(Roles = "Admin")]
public class AuditoriaController : Controller
{
    private readonly AuditoriaRepository _repository;

    public AuditoriaController(AuditoriaRepository repository)
    {
        _repository = repository;
    }

    // Lista registros de auditoría con filtros avanzados y paginación
    public IActionResult Index(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? tabla = null, string? accion = null, int? empleadoId = null, string? busqueda = null, int pagina = 1)
    {
        int tamPagina = 15;
        var registros = _repository.Listar(fechaInicio, fechaFin, tabla, accion, empleadoId, busqueda, pagina, tamPagina);
        int totalRegistros = _repository.Contar(fechaInicio, fechaFin, tabla, accion, empleadoId, busqueda);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamPagina);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.TotalRegistros = totalRegistros;
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
        ViewBag.Tabla = tabla;
        ViewBag.Accion = accion;
        ViewBag.EmpleadoId = empleadoId;
        ViewBag.Busqueda = busqueda;
        ViewBag.Tablas = _repository.ObtenerTablas();
        ViewBag.Empleados = _repository.ObtenerEmpleados();

        return View(registros);
    }

    // Genera un reporte de auditoría con los filtros aplicados
    public IActionResult Reporte(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? tabla = null, string? accion = null, int? empleadoId = null, string? busqueda = null)
    {
        var registros = _repository.Listar(fechaInicio, fechaFin, tabla, accion, empleadoId, busqueda, 1, 1000);

        ViewBag.FechaInicio = fechaInicio?.ToString("dd/MM/yyyy");
        ViewBag.FechaFin = fechaFin?.ToString("dd/MM/yyyy");
        ViewBag.Tabla = tabla;
        ViewBag.Accion = accion;
        ViewBag.EmpleadoId = empleadoId;
        ViewBag.Busqueda = busqueda;

        return View(registros);
    }
}
