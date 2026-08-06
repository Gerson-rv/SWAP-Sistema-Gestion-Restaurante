using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class TurnoCaja
{
    public int IdTurno { get; set; }

    [Required(ErrorMessage = "El empleado es obligatorio.")]
    [Display(Name = "Empleado")]
    public int IdEmpleado { get; set; }

    [Display(Name = "Fecha Apertura")]
    public DateTime FechaApertura { get; set; } = DateTime.Now;

    [Display(Name = "Fecha Cierre")]
    public DateTime? FechaCierre { get; set; }

    [Required(ErrorMessage = "El monto inicial es obligatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "El monto inicial debe ser mayor o igual a 0.")]
    [Display(Name = "Monto Inicial")]
    public decimal MontoInicial { get; set; } = 0;

    [Range(0, double.MaxValue, ErrorMessage = "El monto de cierre debe ser mayor o igual a 0.")]
    [Display(Name = "Monto Cierre")]
    public decimal? MontoCierre { get; set; }

    [StringLength(200)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // Propiedad de navegación para vistas
    [Display(Name = "Empleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    // Propiedad calculada (no de BD)
    public bool EstaAbierto => !FechaCierre.HasValue;
}