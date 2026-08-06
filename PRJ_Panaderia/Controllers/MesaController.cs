using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Mesas - CRUD y gestión de mesas del restaurante
[Authorize(Roles = "Admin,Cajero,Mozo")]
public class MesaController : Controller
{
    private readonly MesaRepository _repository;

    public MesaController(MesaRepository repository)
    {
        _repository = repository;
    }

    // Lista todas las mesas, opcionalmente filtradas por estado
    public IActionResult Index(string? estado)
    {
        var mesas = string.IsNullOrEmpty(estado)
            ? _repository.Listar()
            : _repository.ListarPorEstado(estado);
        ViewBag.EstadoFiltro = estado;
        return View(mesas);
    }

    // Muestra el formulario para crear una mesa
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    // Valida y guarda una nueva mesa verificando número duplicado
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Mesa mesa)
    {
        if (_repository.ExisteNumero(mesa.Numero))
        {
            ModelState.AddModelError("Numero", "Ya existe una mesa con ese número.");
        }

        if (!ModelState.IsValid)
            return View(mesa);

        _repository.Crear(mesa);
        TempData["Exito"] = "Mesa creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar una mesa existente
    public IActionResult Edit(int id)
    {
        var mesa = _repository.ObtenerPorId(id);
        if (mesa == null) return NotFound();
        return View(mesa);
    }

    // Actualiza los datos de una mesa verificando número duplicado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Mesa mesa)
    {
        if (_repository.ExisteNumero(mesa.Numero, mesa.IdMesa))
        {
            ModelState.AddModelError("Numero", "Ya existe otra mesa con ese número.");
        }

        if (!ModelState.IsValid)
            return View(mesa);

        _repository.Actualizar(mesa);
        TempData["Exito"] = "Mesa actualizada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Elimina una mesa por su ID
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            var resultado = _repository.Eliminar(id);

            if (resultado)
            {
                return Json(new
                {
                    success = true,
                    message = "Mesa eliminada exitosamente."
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "No se puede eliminar la mesa porque no está deshabilitada o no existe."
                });
            }
        }
        catch (SqlException ex) when (ex.Number == 547) // Error de clave foránea
        {
            return Json(new
            {
                success = false,
                message = "No se puede eliminar la mesa porque tiene pedidos asociados."
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Ocurrió un error al eliminar la mesa: " + ex.Message
            });
        }
    }

    // Activa o desactiva una mesa por su ID
    [HttpPost]
    public IActionResult CambiarEstado(int id, bool activo)
    {
        try
        {
            _repository.CambiarEstado(id, activo);
            var mensaje = activo ? "Mesa activada exitosamente." : "Mesa desactivada exitosamente.";
            return Json(new { success = true, message = mensaje });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al cambiar el estado de la mesa." });
        }
    }
}
