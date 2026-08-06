using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Categorías - CRUD y gestión de categorías de productos
[Authorize(Roles = "Admin")]
public class CategoriaController : Controller
{
    private readonly CategoriaRepository _repository;

    public CategoriaController(CategoriaRepository repository)
    {
        _repository = repository;
    }

    // Lista todas las categorías registradas
    public IActionResult Index()
    {
        var categorias = _repository.Listar();
        return View(categorias);
    }

    // Muestra el formulario para crear una categoría
    public IActionResult Create()
    {
        return View();
    }

    // Valida y guarda una nueva categoría
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Categoria categoria)
    {
        if (!ModelState.IsValid)
            return View(categoria);

        _repository.Crear(categoria);
        TempData["Exito"] = "Categoría creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar una categoría existente
    public IActionResult Edit(int id)
    {
        var categoria = _repository.ObtenerPorId(id);
        if (categoria == null) return NotFound();
        return View(categoria);
    }

    // Actualiza los datos de una categoría existente
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Categoria categoria)
    {
        if (!ModelState.IsValid)
            return View(categoria);

        _repository.Actualizar(categoria);
        TempData["Exito"] = "Categoría actualizada exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Elimina una categoría por su ID
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            _repository.Eliminar(id);

            return Json(new
            {
                success = true,
                message = "Categoría eliminada exitosamente."
            });
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return Json(new
            {
                success = false,
                message = "No se puede eliminar la categoría porque tiene productos o pedidos asociados."
            });
        }
        catch
        {
            return Json(new
            {
                success = false,
                message = "Ocurrió un error al eliminar la categoría."
            });
        }
    }
}
