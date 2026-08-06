using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Producto
{
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Display(Name = "Precio")]
    public decimal Precio { get; set; }

    [StringLength(500)]
    [Display(Name = "Imagen")]
    public string? RutaImagen { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de Creación")]
    public DateTime FechaCreacion { get; set; }

    [Display(Name = "Categoría")]
    public string NombreCategoria { get; set; } = string.Empty;
}
