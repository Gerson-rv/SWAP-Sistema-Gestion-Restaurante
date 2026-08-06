using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;
using PRJ_Panaderia.Models.ViewModels;

namespace PRJ_Panaderia.Controllers;

// Controlador de Home - Dashboard principal con métricas del día
[Authorize]
public class HomeController : Controller
{
    private readonly DashboardRepository _repository;

    public HomeController(DashboardRepository repository)
    {
        _repository = repository;
    }

    // Muestra el dashboard con ventas, pedidos y métricas del día
    public IActionResult Index(int top = 5)
    {
        var model = new DashboardViewModel
        {
            VentasHoy = _repository.ObtenerVentasHoy(),
            PedidosHoy = _repository.ObtenerPedidosHoy(),
            ClientesHoy = _repository.ObtenerClientesHoy(),
            IngresosHoy = _repository.ObtenerVentasHoy(),
            UltimosPedidos = _repository.ObtenerUltimosPedidos(top),
            PlatosTop = _repository.ObtenerPlatosTop(5),
            Ventas7Dias = _repository.ObtenerVentas7Dias(),
            Mesas = _repository.ObtenerMesas(),
            ItemsPorPagina = top
        };
        return View(model);
    }

    // Muestra la página de error con el identificador de la solicitud
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
