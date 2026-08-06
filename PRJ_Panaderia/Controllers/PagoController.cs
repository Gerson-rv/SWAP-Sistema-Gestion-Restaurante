using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;
using PRJ_Panaderia.Services;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace PRJ_Panaderia.Controllers;

// Controlador de Pagos - Registro, comprobantes y gestión de pagos
[Authorize(Roles = "Admin,Cajero")]
public class PagoController : Controller
{
    private readonly PagoRepository _repository;
    private readonly PedidoRepository _pedidoRepo;
    private readonly DetallePedidoRepository _detalleRepo;
    private readonly ConfiguracionSistemaRepository _configRepo;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _emailService;

    public PagoController(PagoRepository repository, PedidoRepository pedidoRepo, DetallePedidoRepository detalleRepo,
        ConfiguracionSistemaRepository configRepo, IWebHostEnvironment env, EmailService emailService)
    {
        _repository = repository;
        _pedidoRepo = pedidoRepo;
        _detalleRepo = detalleRepo;
        _configRepo = configRepo;
        _env = env;
        _emailService = emailService;
    }

    // Lista pedidos pendientes de pago con filtros y paginación
    public IActionResult Listado(string? busqueda = null, int? mesaId = null, int? empleadoId = null, int pagina = 1)
    {
        int tamPagina = 10;
        var pedidos = _repository.ObtenerPedidosPendientes(busqueda, mesaId, empleadoId, pagina, tamPagina);
        int totalRegistros = _repository.ContarPedidosPendientes(busqueda, mesaId, empleadoId);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamPagina);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.Busqueda = busqueda;
        ViewBag.MesaId = mesaId;
        ViewBag.EmpleadoId = empleadoId;
        ViewBag.Mesas = _repository.ObtenerMesas();
        ViewBag.Empleados = _repository.ObtenerEmpleados();

        return View(pedidos);
    }

    // Muestra el formulario para registrar el pago de un pedido
    public IActionResult Registrar(int pedidoId)
    {
        var pedido = _repository.ObtenerPedido(pedidoId);
        if (pedido == null) return NotFound();

        List<DetallePedido> detalles;
        try
        {
            var (_, d) = _detalleRepo.ObtenerConDetalles(pedidoId);
            detalles = d ?? new List<DetallePedido>();
        }
        catch
        {
            detalles = new List<DetallePedido>();
        }

        var numeroYape = _repository.ObtenerNumeroYape();

        ViewBag.Pedido = pedido;
        ViewBag.Detalles = detalles;
        ViewBag.NumeroYape = numeroYape;

        var pago = new Pago
        {
            PedidoID = pedidoId,
            Monto = pedido.Total,
            Metodo = "Efectivo"
        };

        return View(pago);
    }

    // Procesa el pago, calcula vuelto y guarda comprobante QR si aplica
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registrar(Pago pago)
    {
        var pedido = _repository.ObtenerPedido(pago.PedidoID);
        if (pedido == null) return NotFound();

        if (_repository.TienePagoConfirmado(pago.PedidoID))
            return BadRequest(new { success = false, message = "Este pedido ya tiene un pago confirmado. Anule el pago anterior si desea registrar uno nuevo." });

        if (pago.Metodo == "Efectivo" && (!pago.MontoRecibido.HasValue || pago.MontoRecibido < pago.Monto))
        {
            ModelState.AddModelError("MontoRecibido", "El monto recibido debe ser mayor o igual al total.");
            var (_, detalles) = _detalleRepo.ObtenerConDetalles(pago.PedidoID);
            ViewBag.Pedido = pedido;
            ViewBag.Detalles = detalles;
            ViewBag.NumeroYape = _repository.ObtenerNumeroYape();
            return View(pago);
        }

        pago.Fecha = DateTime.Now;
        pago.Estado = "Confirmado";

        if (pago.Metodo == "Efectivo" && pago.MontoRecibido.HasValue)
        {
            pago.Vuelto = pago.MontoRecibido.Value - pago.Monto;
        }
        else
        {
            pago.Vuelto = 0;
        }

        if (pago.Metodo == "Yape" && !string.IsNullOrEmpty(pago.QR_Bytes))
        {
            try
            {
                var qrFolder = Path.Combine(_env.WebRootPath, "QRs");
                if (!Directory.Exists(qrFolder))
                    Directory.CreateDirectory(qrFolder);

                var qrFileName = $"Yape_{pago.PedidoID}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var qrPath = Path.Combine(qrFolder, qrFileName);
                var qrBytes = Convert.FromBase64String(pago.QR_Bytes);
                System.IO.File.WriteAllBytes(qrPath, qrBytes);
                pago.QR_Ruta = $"/QRs/{qrFileName}";
            }
            catch (FormatException)
            {
                return BadRequest(new { success = false, message = "Error al procesar el código QR." });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false, message = "Error al guardar el código QR." });
            }
        }

        var pagoId = _repository.Crear(pago);

        _pedidoRepo.MarcarPagado(pago.PedidoID);

        return RedirectToAction(nameof(Comprobante), new { pagoId });
    }

    // Registra el pago via AJAX y retorna JSON con el pagoId
    [HttpPost]
    public async Task<JsonResult> RegistrarAjax(Pago pago)
    {
        var pedido = _repository.ObtenerPedido(pago.PedidoID);
        if (pedido == null)
            return Json(new { success = false, message = "Pedido no encontrado." });

        if (_repository.TienePagoConfirmado(pago.PedidoID))
            return Json(new { success = false, message = "Este pedido ya tiene un pago confirmado. Anule el pago anterior si desea registrar uno nuevo." });

        if (pago.Metodo == "Efectivo" && (!pago.MontoRecibido.HasValue || pago.MontoRecibido < pago.Monto))
            return Json(new { success = false, message = "El monto recibido debe ser mayor o igual al total." });

        pago.Fecha = DateTime.Now;
        pago.Estado = "Confirmado";

        if (pago.Metodo == "Efectivo" && pago.MontoRecibido.HasValue)
            pago.Vuelto = pago.MontoRecibido.Value - pago.Monto;
        else
            pago.Vuelto = 0;

        if (pago.Metodo == "Yape" && !string.IsNullOrEmpty(pago.QR_Bytes))
        {
            try
            {
                var qrFolder = Path.Combine(_env.WebRootPath, "QRs");
                if (!Directory.Exists(qrFolder))
                    Directory.CreateDirectory(qrFolder);

                var qrFileName = $"Yape_{pago.PedidoID}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var qrPath = Path.Combine(qrFolder, qrFileName);
                var qrBytes = Convert.FromBase64String(pago.QR_Bytes);
                System.IO.File.WriteAllBytes(qrPath, qrBytes);
                pago.QR_Ruta = $"/QRs/{qrFileName}";
            }
            catch (FormatException)
            {
                return Json(new { success = false, message = "Error al procesar el código QR." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al guardar el código QR." });
            }
        }

        var pagoId = _repository.Crear(pago);
        _pedidoRepo.MarcarPagado(pago.PedidoID);

        var emailSent = false;
        var emailTo = "";
        var emailMessage = "";

        if (pago.Metodo == "Yape")
        {
            var config = _configRepo.Obtener();
            if (config != null && !string.IsNullOrWhiteSpace(config.Correo))
            {
                emailTo = config.Correo;
                var numeroYape = _repository.ObtenerNumeroYape() ?? "";

                var (success, message) = await _emailService.EnviarNotificacionYape(
                    emailTo,
                    config.NombreNegocio,
                    config.RazonSocial,
                    pago.Monto,
                    numeroYape,
                    pago.Fecha,
                    pagoId + 100);

                emailSent = success;
                emailMessage = message;
            }
            else
            {
                emailMessage = "No hay correo configurado en el sistema.";
            }
        }

        return Json(new
        {
            success = true,
            pagoId = pagoId,
            message = "Pago registrado exitosamente.",
            emailSent,
            emailTo,
            emailMessage
        });
    }

    // Muestra el comprobante de pago generado
    public IActionResult Comprobante(int pagoId)
    {
        var pago = _repository.ObtenerPorId(pagoId);
        if (pago == null) return NotFound();

        var pedido = _repository.ObtenerPedido(pago.PedidoID);
        if (pedido == null) return NotFound();

        List<DetallePedido> detalles;
        try
        {
            var (_, d) = _detalleRepo.ObtenerConDetalles(pago.PedidoID);
            detalles = d ?? new List<DetallePedido>();
        }
        catch
        {
            detalles = new List<DetallePedido>();
        }

        ViewBag.Pedido = pedido;
        ViewBag.Detalles = detalles;

        return View(pago);
    }

    // Lista historial de pagos con filtros de fecha, método y paginación
    public IActionResult Historial(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? metodo = null, string? busqueda = null, int pagina = 1)
    {
        int tamPagina = 10;
        var pagos = _repository.Listar(fechaInicio, fechaFin, metodo, busqueda, pagina, tamPagina);
        int totalRegistros = _repository.Contar(fechaInicio, fechaFin, metodo, busqueda);
        int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamPagina);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
        ViewBag.Metodo = metodo;
        ViewBag.Busqueda = busqueda;
        ViewBag.Mesas = _repository.ObtenerMesas();

        return View(pagos);
    }

    // Anula un pago y devuelve el pedido a estado pendiente
    [HttpPost]
    public IActionResult Anular(int pagoId)
    {
        try
        {
            _repository.Anular(pagoId);
            return Json(new { success = true, message = "Pago anulado exitosamente. El pedido volvió a estado Pendiente." });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Error al anular el pago." });
        }
    }

    // Genera código QR con el número de Yape configurado
    [HttpPost]
    public JsonResult GenerarQR()
    {
        var numeroYape = _repository.ObtenerNumeroYape();
        if (string.IsNullOrEmpty(numeroYape))
            return Json(new { success = false, message = "No hay número Yape configurado." });

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(numeroYape, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(20);

        return Json(new { success = true, qr = Convert.ToBase64String(qrBytes) });
    }
}
