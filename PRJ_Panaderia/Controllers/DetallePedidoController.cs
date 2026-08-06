using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Controllers;

// Controlador de Detalle de Pedidos - Consulta y gestión del detalle de pedidos
[Authorize(Roles = "Admin,Cajero,Mozo")]
public class DetallePedidoController : Controller
{
    private readonly DetallePedidoRepository _repository;
    private readonly PedidoRepository _pedidoRepo;

    public DetallePedidoController(DetallePedidoRepository repository, PedidoRepository pedidoRepo)
    {
        _repository = repository;
        _pedidoRepo = pedidoRepo;
    }

    // Lista pedidos con filtros de mesa, estado entrega, estado pago y búsqueda paginada
    public IActionResult Index(int? mesaId = null, string? estadoEntrega = null, string? estadoPago = null, string? busqueda = null, int pagina = 1)
    {
        int tamPagina = 10;
        var pedidos = _repository.Listar(mesaId, estadoEntrega, estadoPago, busqueda, pagina, tamPagina);
        int totalRegistros = _repository.Contar(mesaId, estadoEntrega, estadoPago, busqueda);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamPagina);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.MesaId = mesaId;
        ViewBag.EstadoEntrega = estadoEntrega;
        ViewBag.EstadoPago = estadoPago;
        ViewBag.Busqueda = busqueda;
        ViewBag.Mesas = _repository.ObtenerMesas();

        return View(pedidos);
    }

    // Muestra el detalle completo de un pedido con sus ítems
    public IActionResult Detalle(int pedidoId)
    {
        var (pedido, detalles) = _repository.ObtenerConDetalles(pedidoId);
        if (pedido == null) return NotFound();

        ViewBag.Entregado = _pedidoRepo.EstaCompletoEntregado(pedidoId);

        return View((pedido, detalles));
    }

    // Muestra vista de impresión del pedido para cocina
    public IActionResult Impresion(int pedidoId)
    {
        var (pedido, detalles) = _repository.ObtenerConDetalles(pedidoId);
        if (pedido == null) return NotFound();

        return View((pedido, detalles));
    }

    // Marca un pedido como servido
    [HttpPost]
    public IActionResult MarcarServido(int pedidoId)
    {
        try
        {
            _repository.MarcarComoServido(pedidoId);
            return Json(new { success = true, message = "Pedido marcado como servido." });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al marcar el pedido como servido." });
        }
    }
}
