using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

[Authorize(Roles = "Admin")]
public class ConfiguracionController : Controller
{
    private readonly ConfiguracionSistemaRepository _repository;

    public ConfiguracionController(ConfiguracionSistemaRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        var config = _repository.Obtener();
        if (config == null) return NotFound();
        return View(config);
    }

    public IActionResult Edit()
    {
        var config = _repository.Obtener();
        if (config == null) return NotFound();
        return View(config);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ConfiguracionSistema config)
    {
        if (config.Correo != null)
            config.Correo = config.Correo.Trim();

        if (string.IsNullOrWhiteSpace(config.Correo))
            ModelState.AddModelError("Correo", "El correo del sistema es obligatorio.");
        else if (!Regex.IsMatch(config.Correo, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
            ModelState.AddModelError("Correo", "El correo no tiene un formato valido. Ejemplo: ventas@negocio.com");

        if (!ModelState.IsValid)
            return View(config);

        _repository.Actualizar(config);
        TempData["Exito"] = "Configuracion actualizada exitosamente.";
        return RedirectToAction(nameof(Edit));
    }
}
