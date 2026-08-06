using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;
using PRJ_Panaderia.Models.ViewModels;

namespace PRJ_Panaderia.Controllers;

// Controlador de Pedidos - Creación, consulta y gestión de pedidos
[Authorize(Roles = "Admin,Cajero,Mozo")]
public class PedidoController : Controller
{
    private readonly PedidoRepository _pedidoRepo;
    private readonly ConfiguracionSistemaRepository _configRepo;

    public PedidoController(PedidoRepository pedidoRepo, ConfiguracionSistemaRepository configRepo)
    {
        _pedidoRepo = pedidoRepo;
        _configRepo = configRepo;
    }

    // Muestra la vista principal de pedidos con filtros y datos iniciales
    public IActionResult Index(int? mesaId = null, int? empleadoId = null)
    {
        var config = _configRepo.Obtener();
        var turnoAbierto = _pedidoRepo.ObtenerTurnoAbierto();

        var empleadoActual = empleadoId;
        if (!empleadoActual.HasValue)
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimId, out int id))
                empleadoActual = id;
        }

        var viewModel = new PedidoViewModel
        {
            MesaID = mesaId,
            EmpleadoID = empleadoActual ?? 0,
            CategoriasActivas = _pedidoRepo.ObtenerCategoriasActivas(),
            ProductosDisponibles = _pedidoRepo.ObtenerProductosActivos(),
            MesasDisponibles = _pedidoRepo.ObtenerMesasDisponibles(),
            EmpleadosActivos = _pedidoRepo.ObtenerEmpleadosActivos(),
            IGV_Porcentaje = config?.IGV_Porcentaje ?? 18.00m,
            Moneda = config?.Moneda ?? "PEN",
            HistorialPedidos = _pedidoRepo.Listar(estado: "Pendiente"),
            TurnoAbiertoID = turnoAbierto?.IdTurno
        };
        return View(viewModel);
    }

    // Retorna productos activos en formato JSON, filtrados por categoría
    [HttpGet]
    public JsonResult ObtenerProductos(int? idCategoria = null)
    {
        var productos = _pedidoRepo.ObtenerProductosActivos(idCategoria);
        return Json(productos);
    }

    // Retorna categorías activas en formato JSON
    [HttpGet]
    public JsonResult ObtenerCategorias()
    {
        var categorias = _pedidoRepo.ObtenerCategoriasActivas();
        return Json(categorias);
    }

    // Retorna mesas disponibles en formato JSON
    [HttpGet]
    public JsonResult ObtenerMesas()
    {
        var mesas = _pedidoRepo.ObtenerMesasDisponibles();
        return Json(mesas);
    }

    // Retorna empleados activos en formato JSON
    [HttpGet]
    public JsonResult ObtenerEmpleados()
    {
        var empleados = _pedidoRepo.ObtenerEmpleadosActivos();
        return Json(empleados);
    }

    // Retorna la configuración de IGV y moneda del sistema
    [HttpGet]
    public JsonResult ObtenerIGV()
    {
        var config = _configRepo.Obtener();
        return Json(new
        {
            igvPorcentaje = config?.IGV_Porcentaje ?? 18.00m,
            moneda = config?.Moneda ?? "PEN"
        });
    }

    // Crea un nuevo pedido con sus detalles y calcula IGV
    [HttpPost]
    public JsonResult CrearPedido([FromBody] CrearPedidoRequest request)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                return Json(new { success = false, message = "El pedido debe tener al menos 1 producto." });

            if (!_pedidoRepo.TurnoEstaAbierto(request.TurnoID))
                return Json(new { success = false, message = "El turno de caja no está abierto. Abra un turno antes de crear pedidos." });

            if (request.MesaID.HasValue && _pedidoRepo.TienePedidoPendienteEnMesa(request.MesaID.Value))
                return Json(new { success = false, message = "Esta mesa ya tiene un pedido activo. Finalice o cancele el pedido anterior primero." });

            var config = _configRepo.Obtener();
            var igvPorcentaje = config?.IGV_Porcentaje ?? 18.00m;

            decimal total = request.Items.Sum(i => i.Cantidad * i.PrecioUnitario);
            decimal factor = 1 + (igvPorcentaje / 100);
            decimal subtotal = Math.Round(total / factor, 2);
            decimal igv = Math.Round(total - subtotal, 2);

            var pedido = new Pedido
            {
                TurnoID = request.TurnoID,
                EmpleadoID = request.EmpleadoID,
                MesaID = request.MesaID,
                TipoServicio = request.TipoServicio,
                Estado = "Pendiente",
                Subtotal = subtotal,
                IGV = igv,
                Total = total,
                NotasEspeciales = request.NotasEspeciales
            };

            var detalles = request.Items.Select(i => new DetallePedido
            {
                ProductoID = i.ProductoID,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Modificadores = i.Modificadores
            }).ToList();

            var pedidoId = _pedidoRepo.CrearPedido(pedido, detalles);

            return Json(new
            {
                success = true,
                message = $"Pedido creado exitosamente.",
                pedidoId = pedidoId
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al crear el pedido: " + ex.Message });
        }
    }

    // Marca un pedido como pagado
    [HttpPost]
    public JsonResult MarcarPagado(int id)
    {
        try
        {
            _pedidoRepo.MarcarPagado(id);
            return Json(new { success = true, message = "Pedido marcado como pagado." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al marcar pedido: " + ex.Message });
        }
    }

    // Anula un pedido existente
    [HttpPost]
    public JsonResult Anular(int id)
    {
        try
        {
            _pedidoRepo.Anular(id);
            return Json(new { success = true, message = "Pedido anulado exitosamente." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al anular pedido: " + ex.Message });
        }
    }

    // Retorna el detalle completo de un pedido en formato JSON
    [HttpGet]
    public JsonResult ObtenerDetallePedido(int id)
    {
        var pedido = _pedidoRepo.ObtenerPorId(id);
        if (pedido == null)
            return Json(new { success = false, message = "Pedido no encontrado." });

        var detalles = _pedidoRepo.ObtenerDetalles(id);
        return Json(new
        {
            success = true,
            pedido = new
            {
                pedido.PedidoID,
                pedido.FechaHora,
                pedido.TipoServicio,
                pedido.Estado,
                pedido.Subtotal,
                pedido.IGV,
                pedido.Total,
                pedido.NotasEspeciales,
                pedido.NumeroMesa,
                pedido.NombreEmpleado
            },
            detalles = detalles.Select(d => new
            {
                d.NombreProducto,
                d.Cantidad,
                d.PrecioUnitario,
                d.Modificadores,
                Subtotal = d.Subtotal
            })
        });
    }


    // Retorna historial de pedidos filtrado por mesa, estado y fechas
    [HttpPost]
    public JsonResult ObtenerHistorial([FromBody] HistorialRequest request)
    {
        var pedidos = _pedidoRepo.Listar(
            mesaId: request.MesaId,
            estado: request.Estado,
            fechaInicio: request.FechaInicio,
            fechaFin: request.FechaFin
        );
        return Json(pedidos);
    }

    // Valida si una mesa tiene pedido activo
    [HttpGet]
    public JsonResult ValidarMesaActiva(int mesaId)
    {
        var tieneActivo = _pedidoRepo.TienePedidoPendienteEnMesa(mesaId);
        return Json(new { tieneActivo = tieneActivo });
    }

}

public class CrearPedidoRequest
{
    public int TurnoID { get; set; }
    public int EmpleadoID { get; set; }
    public int? MesaID { get; set; }
    public string TipoServicio { get; set; } = "Mesa";
    public string? NotasEspeciales { get; set; }
    public List<ItemPedidoRequest> Items { get; set; } = new();
}

public class ItemPedidoRequest
{
    public int ProductoID { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Modificadores { get; set; }
}

public class HistorialRequest
{
    public int? MesaId { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
