using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Auditoria
{
    public int AuditoriaID { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Tabla")]
    public string Tabla { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Registro ID")]
    public int RegistroID { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Accion")]
    public string Accion { get; set; } = string.Empty;

    [Display(Name = "Empleado")]
    public int? EmpleadoID { get; set; }

    [Required]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [StringLength(400)]
    [Display(Name = "Detalle")]
    public string? Detalle { get; set; }

    [Display(Name = "Empleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    public string NombreCargo { get; set; } = string.Empty;
}
