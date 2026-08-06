using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Categoria
{
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
