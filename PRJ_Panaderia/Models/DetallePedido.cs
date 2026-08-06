using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class DetallePedido
{
    public int DetalleID { get; set; }

    [Required]
    [Display(Name = "Pedido")]
    public int PedidoID { get; set; }

    [Required]
    [Display(Name = "Producto")]
    public int ProductoID { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; } = 1;

    [Required]
    [Display(Name = "Precio Unitario")]
    public decimal PrecioUnitario { get; set; }

    [StringLength(120)]
    [Display(Name = "Modificadores")]
    public string? Modificadores { get; set; }

    [Display(Name = "Entregado")]
    public bool Entregado { get; set; } = false;

    [Display(Name = "Producto")]
    public string NombreProducto { get; set; } = string.Empty;

    [Display(Name = "Subtotal")]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}
