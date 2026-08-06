using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class Pedido
{
    public int PedidoID { get; set; }

    [Required(ErrorMessage = "El turno es obligatorio.")]
    [Display(Name = "Turno")]
    public int TurnoID { get; set; }

    [Display(Name = "Cliente")]
    public int? ClienteID { get; set; }

    [Required(ErrorMessage = "El empleado es obligatorio.")]
    [Display(Name = "Empleado")]
    public int EmpleadoID { get; set; }

    [Display(Name = "Mesa")]
    public int? MesaID { get; set; }

    [Display(Name = "Fecha y Hora")]
    public DateTime FechaHora { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "El tipo de servicio es obligatorio.")]
    [Display(Name = "Tipo de Servicio")]
    public string TipoServicio { get; set; } = "Mesa";

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Pendiente";

    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; } = 0;

    [Display(Name = "IGV")]
    public decimal IGV { get; set; } = 0;

    [Display(Name = "Total")]
    public decimal Total { get; set; } = 0;

    [StringLength(300)]
    [Display(Name = "Notas Especiales")]
    public string? NotasEspeciales { get; set; }

    [Display(Name = "N° Mesa")]
    public int NumeroMesa { get; set; }

    [Display(Name = "Empleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    [Display(Name = "Cliente")]
    public string? NombreCliente { get; set; }

    [Display(Name = "Estado de Entrega")]
    public string EstadoEntrega { get; set; } = "Pendiente";
}
