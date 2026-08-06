using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Empleado
{
    public int IdEmpleado { get; set; }

    [Required(ErrorMessage = "El cargo es obligatorio.")]
    [Display(Name = "Cargo")]
    public int IdCargo { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(70)]
    [RegularExpression(@"^[a-zA-ZáéíóúñüÁÉÍÓÚÑÜ\s'-]+$", ErrorMessage = "El nombre no debe contener números ni caracteres especiales.")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener exactamente 8 dígitos.")]
    [Display(Name = "DNI")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(64)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [RegularExpression(@"^9\d{8}$", ErrorMessage = "El teléfono debe tener 9 dígitos y comenzar con 9.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de Creación")]
    public DateTime FechaCreacion { get; set; }

    [Display(Name = "Cargo")]
    public string NombreCargo { get; set; } = string.Empty;
}
