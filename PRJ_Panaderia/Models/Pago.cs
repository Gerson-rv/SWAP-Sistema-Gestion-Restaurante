using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Pago
{
    public int PagoID { get; set; }

    [Required(ErrorMessage = "El pedido es obligatorio.")]
    [Display(Name = "Pedido")]
    public int PedidoID { get; set; }

    [Required(ErrorMessage = "El método de pago es obligatorio.")]
    [Display(Name = "Método de Pago")]
    public string Metodo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [Display(Name = "Monto Recibido")]
    public decimal? MontoRecibido { get; set; }

    [Display(Name = "Vuelto")]
    public decimal? Vuelto { get; set; }

    [Display(Name = "Ruta QR")]
    public string? QR_Ruta { get; set; }

    [Display(Name = "QR Bytes")]
    public string? QR_Bytes { get; set; }

    [Display(Name = "Fecha de Pago")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Confirmado";

    [Display(Name = "N° Mesa")]
    public int NumeroMesa { get; set; }

    [Display(Name = "Empleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    [Display(Name = "Comanda")]
    public string NumeroComanda { get; set; } = string.Empty;
}
