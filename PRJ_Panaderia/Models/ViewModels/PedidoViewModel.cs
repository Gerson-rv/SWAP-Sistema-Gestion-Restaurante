using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models.ViewModels;

public class PedidoViewModel
{
    [Display(Name = "Mesa")]
    public int? MesaID { get; set; }

    [Display(Name = "Empleado")]
    public int EmpleadoID { get; set; }

    [Display(Name = "Turno")]
    public int TurnoID { get; set; }

    [Display(Name = "Tipo de Servicio")]
    public string TipoServicio { get; set; } = "Mesa";

    [Display(Name = "Notas Especiales")]
    public string? NotasEspeciales { get; set; }

    public List<Mesa> MesasDisponibles { get; set; } = new();
    public List<Empleado> EmpleadosActivos { get; set; } = new();
    public List<Categoria> CategoriasActivas { get; set; } = new();
    public List<Producto> ProductosDisponibles { get; set; } = new();
    public List<DetallePedido> Carrito { get; set; } = new();

    public decimal Subtotal => Carrito.Sum(d => d.Subtotal);
    public decimal IGV_Porcentaje { get; set; } = 18.00m;
    public decimal IGV => Math.Round(Subtotal * IGV_Porcentaje / 100, 2);
    public decimal Total => Subtotal + IGV;
    public string Moneda { get; set; } = "PEN";
    public string SimboloMoneda => Moneda == "USD" ? "$" : "S/";

    public List<Pedido> HistorialPedidos { get; set; } = new();

    public int? TurnoAbiertoID { get; set; }
}
