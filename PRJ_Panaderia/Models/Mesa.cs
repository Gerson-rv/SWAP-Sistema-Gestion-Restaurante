using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Mesa
{
    public int IdMesa { get; set; }

    [Required(ErrorMessage = "El número de mesa es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El número debe ser mayor a 0.")]
    [Display(Name = "Número")]
    public int Numero { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Libre";

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
