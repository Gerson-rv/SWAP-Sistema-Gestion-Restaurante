namespace PRJ_Panaderia.Models.ViewModels;

public class DashboardViewModel
{
    public decimal VentasHoy { get; set; }
    public int PedidosHoy { get; set; }
    public int ClientesHoy { get; set; }
    public decimal IngresosHoy { get; set; }
    public List<UltimoPedido> UltimosPedidos { get; set; } = new();
    public List<ProductoTop> PlatosTop { get; set; } = new();
    public List<VentasDia> Ventas7Dias { get; set; } = new();
    public List<MesaEstado> Mesas { get; set; } = new();
    public int ItemsPorPagina { get; set; } = 5;
}

public class UltimoPedido
{
    public int PedidoID { get; set; }
    public int? NumeroMesa { get; set; }
    public string? NombreCliente { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
    public string TipoServicio { get; set; } = "";
}

public class ProductoTop
{
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
}

public class VentasDia
{
    public string DiaAbrev { get; set; } = "";
    public string Fecha { get; set; } = "";
    public decimal Total { get; set; }
}

public class MesaEstado
{
    public int IdMesa { get; set; }
    public int Numero { get; set; }
    public string Estado { get; set; } = "";
    public string? NombreEmpleado { get; set; }
}
