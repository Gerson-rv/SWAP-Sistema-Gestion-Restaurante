using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Productos - CRUD y gestión de productos con imágenes
[Authorize(Roles = "Admin,Cajero,Mozo")]
public class ProductoController : Controller
{
    private readonly ProductoRepository _repository;
    private readonly IWebHostEnvironment _enviroment;
    private readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png" };
    private const long _tamanoMaximo = 5 * 1024 * 1024; // 5 MB

    public ProductoController(ProductoRepository repository, IWebHostEnvironment enviroment)
    {
        _repository = repository;
        _enviroment = enviroment;
    }

    // Lista todos los productos con su categoría
    public IActionResult Index()
    {
        var productos = _repository.Listar();
        return View(productos);
    }

    // Muestra el formulario para crear un producto
    [Authorize(Roles = "Admin,Cajero")]
    public IActionResult Create()
    {
        ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
        return View();
    }

    // Valida y guarda un nuevo producto con imagen
    [HttpPost]
    [Authorize(Roles = "Admin,Cajero")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Producto producto, IFormFile? archivoImagen)
    {
        if (archivoImagen == null || archivoImagen.Length == 0)
        {
            ModelState.AddModelError("RutaImagen", "La imagen del producto es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
            return View(producto);
        }

        var validar = ValidarImagen(archivoImagen!);
        if (validar != null)
        {
            ModelState.AddModelError("RutaImagen", validar);
            ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
            return View(producto);
        }

        producto.RutaImagen = GuardarImagen(archivoImagen!);
        _repository.Crear(producto);
        TempData["Exito"] = "Producto creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar un producto existente
    [Authorize(Roles = "Admin,Cajero")]
    public IActionResult Edit(int id)
    {
        var producto = _repository.ObtenerPorId(id);
        if (producto == null) return NotFound();
        ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
        return View(producto);
    }

    // Actualiza un producto y reemplaza su imagen si se proporciona una nueva
    [HttpPost]
    [Authorize(Roles = "Admin,Cajero")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Producto producto, IFormFile? archivoImagen)
    {
        if (archivoImagen == null || archivoImagen.Length == 0)
        {
            if (string.IsNullOrEmpty(producto.RutaImagen))
            {
                ModelState.AddModelError("RutaImagen", "La imagen del producto es obligatoria.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
            return View(producto);
        }

        if (archivoImagen != null && archivoImagen.Length > 0)
        {
            var validar = ValidarImagen(archivoImagen);
            if (validar != null)
            {
                ModelState.AddModelError("RutaImagen", validar);
                ViewBag.Categorias = new SelectList(_repository.ObtenerCategoriasActivas(), "IdCategoria", "Nombre");
                return View(producto);
            }

            if (!string.IsNullOrEmpty(producto.RutaImagen))
            {
                EliminarImagenFisica(producto.RutaImagen);
            }

            producto.RutaImagen = GuardarImagen(archivoImagen);
        }

        _repository.Actualizar(producto);
        TempData["Exito"] = "Producto actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Valida si un producto puede ser eliminado
    [HttpPost]
    public IActionResult ValidarEliminar(int id)
    {
        var total = _repository.ContarDetallesPedido(id);
        if (total > 0)
            return Json(new { success = false, message = $"No se puede eliminar: este producto tiene {total} registro(s) en pedidos. Desactívelo en su lugar." });
        return Json(new { success = true });
    }

    // Elimina un producto y su imagen asociada del disco
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var total = _repository.ContarDetallesPedido(id);
        if (total > 0)
            return Json(new { success = false, message = $"No se puede eliminar: este producto tiene {total} registro(s) en pedidos. Desactívelo en su lugar." });

        var producto = _repository.ObtenerPorId(id);
        if (producto != null && !string.IsNullOrEmpty(producto.RutaImagen))
        {
            EliminarImagenFisica(producto.RutaImagen);
        }

        _repository.Eliminar(id);
        return Json(new { success = true, message = "Producto eliminado exitosamente." });
    }

    // Valida extensión y tamaño de la imagen del producto
    private string? ValidarImagen(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (!_extensionesPermitidas.Contains(extension))
        {
            return "Solo se permiten archivos JPG, JPEG o PNG.";
        }

        if (archivo.Length > _tamanoMaximo)
        {
            return "El archivo no debe superar los 5 MB.";
        }

        return null;
    }

    // Guarda la imagen del producto en disco con nombre único
    private string GuardarImagen(IFormFile archivo)
    {
        var carpeta = Path.Combine(_enviroment.WebRootPath, "Images", "Productos");

        if (!Directory.Exists(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        var nombreArchivo = Guid.NewGuid().ToString("N") + Path.GetExtension(archivo.FileName);
        var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

        using var stream = new FileStream(rutaCompleta, FileMode.Create);
        archivo.CopyTo(stream);

        return Path.Combine("Images", "Productos", nombreArchivo);
    }

    // Elimina el archivo físico de la imagen del producto
    private void EliminarImagenFisica(string rutaRelativa)
    {
        var rutaCompleta = Path.Combine(_enviroment.WebRootPath, rutaRelativa);
        if (System.IO.File.Exists(rutaCompleta))
        {
            System.IO.File.Delete(rutaCompleta);
        }
    }
}
