using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRJ_Panaderia.Models;

public class Cargo
{
    public int IdCargo { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El sueldo es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El sueldo debe estar entre 0.01 y 999,999.99.")]
    [Display(Name = "Sueldo")]
    [Column(TypeName = "decimal(10,2)")]
    [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Ingrese un monto válido (máximo 2 decimales).")]
    public decimal Sueldo { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}