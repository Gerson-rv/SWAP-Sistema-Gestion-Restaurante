using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Cliente
{
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener exactamente 8 dígitos.")]
    [Display(Name = "DNI")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(50)]
    [RegularExpression(@"^[a-zA-ZáéíóúñüÁÉÍÓÚÑÜ\s'-]+$", ErrorMessage = "El nombre no debe contener números ni caracteres especiales.")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [RegularExpression(@"^9\d{8}$", ErrorMessage = "El teléfono debe tener 9 dígitos y comenzar con 9.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Fecha de Registro")]
    public DateTime FechaRegistro { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
