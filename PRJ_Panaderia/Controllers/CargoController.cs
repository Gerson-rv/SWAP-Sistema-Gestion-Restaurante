using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Cargos - CRUD y gestión de cargos de empleados
[Authorize(Roles = "Admin")]
public class CargoController : Controller
{
    private readonly CargoRepository _repository;

    public CargoController(CargoRepository repository)
    {
        _repository = repository;
    }

    // Lista todos los cargos registrados
    public IActionResult Index()
    {
        var cargos = _repository.Listar();
        return View(cargos);
    }

    // Muestra el formulario para editar el sueldo de un cargo
    public IActionResult Edit(int id)
    {
        var cargo = _repository.ObtenerPorId(id);
        if (cargo == null) return NotFound();
        return View(cargo);
    }

    // Guarda el sueldo actualizado del cargo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, decimal sueldo)
    {
        var cargo = _repository.ObtenerPorId(id);
        if (cargo == null) return NotFound();

        if (sueldo <= 0)
        {
            ModelState.AddModelError("Sueldo", "El sueldo debe ser mayor que cero.");
            cargo.Sueldo = sueldo;
            return View(cargo);
        }

        _repository.ActualizarSueldo(id, sueldo);
        TempData["Exito"] = $"Sueldo de '{cargo.Nombre}' actualizado a S/ {sueldo:N2}.";
        return RedirectToAction(nameof(Index));
    }
}
