using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Empleados - CRUD y gestión de empleados del sistema
[Authorize(Roles = "Admin")]
public class EmpleadoController : Controller
{
    private readonly EmpleadoRepository _repository;

    public EmpleadoController(EmpleadoRepository repository)
    {
        _repository = repository;
    }

    // Lista todos los empleados con su cargo
    public IActionResult Index()
    {
        var empleados = _repository.Listar();
        return View(empleados);
    }

    // Muestra el formulario para crear un empleado
    public IActionResult Create()
    {
        CargarCargos();
        return View();
    }

    // Valida y guarda un nuevo empleado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Empleado empleado)
    {
        if (string.IsNullOrWhiteSpace(empleado.Contrasena))
            ModelState.AddModelError("Contrasena", "La contraseña es obligatoria.");

        if (_repository.ExisteDni(empleado.Dni))
            ModelState.AddModelError("Dni", "Ya existe un empleado con este DNI.");

        if (_repository.ExisteUsuario(empleado.Usuario))
            ModelState.AddModelError("Usuario", "Ya existe un empleado con este usuario.");

        if (!string.IsNullOrWhiteSpace(empleado.Telefono) && _repository.ExisteTelefono(empleado.Telefono))
            ModelState.AddModelError("Telefono", "Ya existe un empleado con este numero de telefono.");

        if (!ModelState.IsValid)
        {
            CargarCargos();
            return View(empleado);
        }

        _repository.Crear(empleado);
        TempData["Exito"] = "Empleado creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Muestra el formulario para editar un empleado existente
    public IActionResult Edit(int id)
    {
        var empleado = _repository.ObtenerPorId(id);
        if (empleado == null) return NotFound();
        CargarCargos();
        return View(empleado);
    }

    // Actualiza los datos de un empleado, opcionalmente cambia contraseña
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Empleado empleado, string? nuevaContrasena)
    {
        var existente = _repository.ObtenerPorId(empleado.IdEmpleado);
        if (existente == null) return NotFound();

        // Si los campos están vacíos (por estar deshabilitados), cargar datos existentes
        if (string.IsNullOrWhiteSpace(empleado.NombreCompleto))
            empleado.NombreCompleto = existente.NombreCompleto;
        if (string.IsNullOrWhiteSpace(empleado.Dni))
            empleado.Dni = existente.Dni;
        if (string.IsNullOrWhiteSpace(empleado.Usuario))
            empleado.Usuario = existente.Usuario;
        if (string.IsNullOrWhiteSpace(empleado.Telefono))
            empleado.Telefono = existente.Telefono;
        if (empleado.IdCargo == 0)
            empleado.IdCargo = existente.IdCargo;

        // Limpiar errores de ModelState ya que se rellenaron los campos vacíos
        ModelState.Clear();

        if (_repository.ExisteDni(empleado.Dni, empleado.IdEmpleado))
            ModelState.AddModelError("Dni", "Ya existe un empleado con este DNI.");

        if (_repository.ExisteUsuario(empleado.Usuario, empleado.IdEmpleado))
            ModelState.AddModelError("Usuario", "Ya existe un empleado con este usuario.");

        if (!string.IsNullOrWhiteSpace(empleado.Telefono) && _repository.ExisteTelefono(empleado.Telefono, empleado.IdEmpleado))
            ModelState.AddModelError("Telefono", "Ya existe un empleado con este numero de telefono.");

        if (!string.IsNullOrWhiteSpace(nuevaContrasena))
        {
            empleado.Contrasena = nuevaContrasena;
        }
        else
        {
            empleado.Contrasena = existente.Contrasena;
        }

        if (!ModelState.IsValid)
        {
            CargarCargos();
            return View(empleado);
        }

        _repository.Actualizar(empleado);
        TempData["Exito"] = "Empleado actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // Activa o desactiva un empleado por su ID
    [HttpPost]
    public IActionResult CambiarEstado(int id, bool activo)
    {
        try
        {
            var empleado = _repository.ObtenerPorId(id);
            if (empleado == null)
                return Json(new { success = false, message = "Empleado no encontrado." });

            empleado.Activo = activo;
            _repository.Actualizar(empleado);
            var mensaje = activo ? "Empleado activado exitosamente." : "Empleado desactivado exitosamente.";
            return Json(new { success = true, message = mensaje });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al cambiar el estado del empleado." });
        }
    }

    // Elimina un empleado por su ID
    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            // Obtener el ID del empleado actual
            var empleadoId = ObtenerEmpleadoIdActual();

            // Verificar que no se esté eliminando a sí mismo
            if (id == empleadoId)
            {
                return Json(new
                {
                    success = false,
                    message = "No puedes eliminar tu propia cuenta de administrador."
                });
            }

            var resultado = _repository.Eliminar(id, empleadoId);

            if (resultado)
            {
                return Json(new
                {
                    success = true,
                    message = "Empleado eliminado exitosamente."
                });
            }
            else
            {
                // Determinar la razón del fallo
                var empleado = _repository.ObtenerPorId(id);
                if (empleado == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "El empleado no existe."
                    });
                }

                // Verificar si es el único administrador
                if (EsUnicoAdmin(id))
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede eliminar al único administrador del sistema."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "No se puede eliminar el empleado porque tiene pedidos asociados o es el administrador actual."
                });
            }
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return Json(new
            {
                success = false,
                message = "No se puede eliminar el empleado porque tiene pedidos asociados."
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Ocurrió un error al eliminar el empleado: " + ex.Message
            });
        }
    }

    // Método auxiliar para obtener el ID del empleado actual
    private int ObtenerEmpleadoIdActual()
    {
        var empleadoIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(empleadoIdClaim) && int.TryParse(empleadoIdClaim, out int id))
            return id;

        return 0;
    }

    // Método auxiliar para verificar si es el único administrador
    private bool EsUnicoAdmin(int id)
    {
        var empleado = _repository.ObtenerPorId(id);
        if (empleado == null || empleado.NombreCargo != "Admin")
            return false;

        var admins = _repository.Listar().Where(e => e.NombreCargo == "Admin" && e.Activo);
        return admins.Count() <= 1;
    }

    // Carga la lista de cargos activos en ViewBag
    private void CargarCargos()
    {
        var cargos = _repository.ObtenerCargosActivos();
        ViewBag.Cargos = new SelectList(cargos, "IdCargo", "Nombre");
    }
}