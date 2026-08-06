using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Services;

public class EmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private static readonly Random _random = new();


    // Constructor que recibe la configuración SMTP y el logger
    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    // Envía una notificación de pago Yape al destinatario especificado
    public async Task<(bool Success, string Message)> EnviarNotificacionYape(
        string destinatario,
        string nombreNegocio,
        string razonSocial,
        decimal monto,
        string numeroYape,
        DateTime fecha,
        int nroOperacion)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", destinatario));
            message.Subject = $"TE ACABAN DE PAGAR! - {nombreNegocio}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = ConstruirHtmlYape(nombreNegocio, razonSocial, monto, numeroYape, fecha, nroOperacion);

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.User, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Notificacion Yape enviada a {Destinatario}", destinatario);
            return (true, "Correo enviado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificacion Yape a {Destinatario}", destinatario);
            return (false, $"Error al enviar correo: {ex.Message}");
        }
    }


    // Construye el contenido HTML del correo de notificación Yape
    private string ConstruirHtmlYape(
        string nombreNegocio, string razonSocial, decimal monto,
        string numeroYape, DateTime fecha, int nroOperacion)
    {
        var titularCuenta = GenerarNombreAleatorio();
        var numeroYapero = GenerarNumeroYape();
        var celularBeneficiario = FormatoYape(numeroYape);
        var fechaFormateada = fecha.ToString("dd 'de' MMMM 'de' yyyy - hh:mm tt",
            new System.Globalization.CultureInfo("es-PE"));

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Views", "Pago", "Email", "YapeNotificacion.html");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template de email no encontrado: {templatePath}");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{NOMBRE_NEGOCIO}}", nombreNegocio)
                    .Replace("{{RAZON_SOCIAL}}", razonSocial)
                    .Replace("{{MONTO}}", monto.ToString("N2"))
                    .Replace("{{TITULAR_CUENTA}}", titularCuenta)
                    .Replace("{{NUMERO_YAPERO}}", numeroYapero)
                    .Replace("{{FECHA}}", fechaFormateada)
                    .Replace("{{CELULAR_BENEFICIARIO}}", celularBeneficiario)
                    .Replace("{{NRO_OPERACION}}", nroOperacion.ToString());

        return html;
    }

    // NOMBRES ALEATORES PARA EL TITULAR DE LA CUENTA YAPE
    private string GenerarNombreAleatorio()
    {
        string[] nombres = { "Juan", "Carlos", "Miguel", "Pedro", "Luis", "Marco", "Andres", "Jorge", "Diego", "Roberto",
                             "Maria", "Ana", "Lucia", "Carmen", "Rosa", "Elena", "Sofia", "Laura", "Claudia", "Teresa" };
        string[] apellidos = { "Garcia", "Lopez", "Martinez", "Rodriguez", "Hernandez", "Gonzalez", "Perez", "Sanchez",
                               "Ramirez", "Torres", "Flores", "Rivera", "Gomez", "Diaz", "Reyes", "Cruz", "Morales", "Ortiz" };
        return $"{nombres[_random.Next(nombres.Length)]} {apellidos[_random.Next(apellidos.Length)]}";
    }

    // NUMERO ALEATORIO DE 9 DIGITOS PARA YAPE
    private string GenerarNumeroYape()
    {
        var digitos = new char[8];
        for (int i = 0; i < 8; i++)
            digitos[i] = (char)('0' + _random.Next(10));
        return $"9{new string(digitos)}";
    }

    // FORMATEO DEL NUMERO DE YAPE PARA MOSTRARLO EN EL EMAIL
    private string FormatoYape(string numeroYape)
    {
        var digits = new string(numeroYape.Where(char.IsDigit).ToArray());
        if (digits.Length == 9)
            return $"{digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 3)}";
        return numeroYape;
    }
}
