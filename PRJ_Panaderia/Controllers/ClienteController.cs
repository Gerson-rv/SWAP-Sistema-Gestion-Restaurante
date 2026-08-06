using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Clientes - CRUD y gestión de clientes
[Authorize(Roles = "Admin,Cajero,Mozo")]
public class ClienteController : Controller
{
    private readonly ClienteRepository _repository;

    public ClienteController(ClienteRepository repository)
    {
        _repository = repository;
    }

    // Lista todos los clientes registrados
    public IActionResult Index()
    {
        var clientes = _repository.Listar();
        return View(clientes);
    }

    // Muestra el formulario para crear un cliente
    public IActionResult Create()
    {
        return View();
    }

    // Valida y guarda un nuevo cliente
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Cliente cliente)
    {
        if (!ModelState.IsValid)
            return View(cliente);

        _repository.Crear(cliente);
        TempData["Exito"] = "Cliente creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar un cliente existente
    public IActionResult Edit(int id)
    {
        var cliente = _repository.ObtenerPorId(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    // Actualiza los datos de un cliente existente
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Cliente cliente)
    {
        if (!ModelState.IsValid)
            return View(cliente);

        _repository.Actualizar(cliente);
        TempData["Exito"] = "Cliente actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Elimina un cliente por su ID
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            _repository.Eliminar(id);
            return Json(new { success = true, message = "Cliente eliminado exitosamente." });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547)
        {
            return Json(new { success = false, message = "No se puede eliminar el cliente porque tiene pedidos asociados." });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al eliminar el cliente." });
        }
    }
}
