using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PRJ_Panaderia.Controllers;

// Controlador de Login - Autenticación y gestión de sesiones
public class LoginController : Controller
{
    private readonly string _connectionString;

    public LoginController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexion no esta configurada.");
    }

    // Muestra el formulario de login
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        return RedirectToAction("Index", "Home");
        return View();
    }

    // Valida credenciales e inicia sesión con cookie de autenticación
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string usuario, string contrasena)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            ViewBag.Error = "Ingrese usuario y contrasena.";
            return View();
        }

        usuario = usuario.Trim();
        contrasena = contrasena.Trim();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT e.IdEmpleado, e.NombreCompleto, e.Contrasena, e.Activo,
                     c.Nombre AS NombreCargo
              FROM Empleado e
              INNER JOIN Cargo c ON e.IdCargo = c.IdCargo
              WHERE e.Usuario = @Usuario", connection);
        command.Parameters.AddWithValue("@Usuario", usuario);
        connection.Open();
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            ViewBag.Error = "Usuario no encontrado.";
            return View();
        }

        var id = reader.GetInt32(0);
        var nombre = reader.GetString(1);
        var contrasenaGuardada = reader.GetString(2);
        var activo = reader.GetBoolean(3);
        var nombreCargo = reader.GetString(4);

        if (!activo)
        {
            ViewBag.Error = "Su cuenta esta desactivada.";
            return View();
        }

        if (contrasena != contrasenaGuardada)
        {
            ViewBag.Error = $"Contrasena incorrecta para el usuario {usuario}.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Role, nombreCargo)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return RedirectToAction("Index", "Home");
    }

    // Cierra la sesión del usuario
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index");
    }
}