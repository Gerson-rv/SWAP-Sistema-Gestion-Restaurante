using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Pagos - Acceso a datos de tabla Pagos
public class PagoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public PagoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista pagos con filtros y paginación
    public List<Pago> Listar(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? metodo = null, string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var pagos = new List<Pago>();
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT pg.PagoID, pg.PedidoID, pg.Metodo, pg.Monto, pg.Vuelto,
                           pg.Fecha, pg.Estado, pg.QR_Ruta,
                           m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado
                    FROM Pagos pg
                    INNER JOIN Pedidos p ON pg.PedidoID = p.PedidoID
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    WHERE 1=1";

        if (fechaInicio.HasValue)
            sql += " AND pg.Fecha >= @FechaInicio";
        if (fechaFin.HasValue)
            sql += " AND pg.Fecha < @FechaFin";
        if (!string.IsNullOrEmpty(metodo))
            sql += " AND pg.Metodo = @Metodo";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (CAST(pg.PedidoID AS VARCHAR) LIKE @Busqueda OR CAST(m.Numero AS VARCHAR) LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda COLLATE SQL_Latin1_General_CP1_CI_AS)";

        sql += " ORDER BY pg.PagoID DESC";
        sql += " OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

        using var command = new SqlCommand(sql, connection);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(metodo))
            command.Parameters.AddWithValue("@Metodo", metodo);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

        command.Parameters.AddWithValue("@Offset", (pagina - 1) * tamPagina);
        command.Parameters.AddWithValue("@Fetch", tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pagos.Add(MapearPago(reader));
        }
        return pagos;
    }

    // SELECT - Cuenta total de pagos con filtros para paginación
    public int Contar(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? metodo = null, string? busqueda = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT COUNT(*)
                    FROM Pagos pg
                    INNER JOIN Pedidos p ON pg.PedidoID = p.PedidoID
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    WHERE 1=1";

        if (fechaInicio.HasValue)
            sql += " AND pg.Fecha >= @FechaInicio";
        if (fechaFin.HasValue)
            sql += " AND pg.Fecha < @FechaFin";
        if (!string.IsNullOrEmpty(metodo))
            sql += " AND pg.Metodo = @Metodo";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (CAST(pg.PedidoID AS VARCHAR) LIKE @Busqueda OR CAST(m.Numero AS VARCHAR) LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda COLLATE SQL_Latin1_General_CP1_CI_AS)";

        using var command = new SqlCommand(sql, connection);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(metodo))
            command.Parameters.AddWithValue("@Metodo", metodo);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Obtiene un pago por ID con datos del pedido
    public Pago? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT pg.PagoID, pg.PedidoID, pg.Metodo, pg.Monto, pg.Vuelto,
                     pg.Fecha, pg.Estado, pg.QR_Ruta,
                     m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado,
                     pg.QR_Bytes
              FROM Pagos pg
              INNER JOIN Pedidos p ON pg.PedidoID = p.PedidoID
              LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
              INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
              WHERE pg.PagoID = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var pago = MapearPago(reader);
            pago.QR_Bytes = reader.IsDBNull(10) ? null : Convert.ToBase64String((byte[])reader.GetValue(10));
            pago.MontoRecibido = pago.Monto + (pago.Vuelto ?? 0);
            return pago;
        }
        return null;
    }

    // INSERT - Crea un nuevo pago y retorna el ID generado
    public int Crear(Pago pago)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO Pagos (PedidoID, Metodo, Monto, Vuelto, QR_Ruta, QR_Bytes, Fecha, Estado)
              VALUES (@PedidoID, @Metodo, @Monto, @Vuelto, @QR_Ruta, @QR_Bytes, @Fecha, @Estado);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@PedidoID", pago.PedidoID);
        command.Parameters.AddWithValue("@Metodo", pago.Metodo);
        command.Parameters.AddWithValue("@Monto", pago.Monto);
        command.Parameters.AddWithValue("@Vuelto", (object?)pago.Vuelto ?? DBNull.Value);
        command.Parameters.AddWithValue("@QR_Ruta", (object?)pago.QR_Ruta ?? DBNull.Value);

        if (!string.IsNullOrEmpty(pago.QR_Bytes) && IsBase64String(pago.QR_Bytes))
            command.Parameters.Add("@QR_Bytes", System.Data.SqlDbType.VarBinary).Value = Convert.FromBase64String(pago.QR_Bytes);
        else
            command.Parameters.Add("@QR_Bytes", System.Data.SqlDbType.VarBinary).Value = DBNull.Value;

        command.Parameters.AddWithValue("@Fecha", pago.Fecha);
        command.Parameters.AddWithValue("@Estado", pago.Estado);
        connection.Open();
        var pagoId = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Pagos", pagoId, "INSERT", null,
            $"Pago por S/{pago.Monto:N2} registrado ({pago.Metodo})");
        return pagoId;
    }

    // UPDATE - Anula un pago y retorna el pedido a estado Pendiente
    public void Anular(int pagoId, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int pedidoId;
            using (var cmdGetPedido = new SqlCommand(
                "SELECT PedidoID FROM Pagos WHERE PagoID = @PagoID", connection, transaction))
            {
                cmdGetPedido.Parameters.AddWithValue("@PagoID", pagoId);
                pedidoId = Convert.ToInt32(cmdGetPedido.ExecuteScalar());
            }

            using (var cmdAnular = new SqlCommand(
                "UPDATE Pagos SET Estado = 'Anulado' WHERE PagoID = @PagoID", connection, transaction))
            {
                cmdAnular.Parameters.AddWithValue("@PagoID", pagoId);
                cmdAnular.ExecuteNonQuery();
            }

            using (var cmdPedido = new SqlCommand(
                "UPDATE Pedidos SET Estado = 'Pendiente' WHERE PedidoID = @PedidoID", connection, transaction))
            {
                cmdPedido.Parameters.AddWithValue("@PedidoID", pedidoId);
                cmdPedido.ExecuteNonQuery();
            }

            transaction.Commit();
            _auditRepo.Registrar("Pagos", pagoId, "ANULAR", empleadoId,
                $"Pago #{pagoId} anulado");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // SELECT - Lista pedidos pendientes de pago con filtros y paginación
    public List<Pedido> ObtenerPedidosPendientes(string? busqueda = null, int? mesaId = null, int? empleadoId = null, int pagina = 1, int tamPagina = 10)
    {
        var pedidos = new List<Pedido>();
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT p.PedidoID, p.TurnoID, p.ClienteID, p.EmpleadoID, p.MesaID,
                           p.FechaHora, p.TipoServicio, p.Estado, p.Subtotal, p.IGV, p.Total, p.NotasEspeciales,
                           m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado,
                           c.NombreCompleto AS NombreCliente
                    FROM Pedidos p
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    LEFT JOIN Cliente c ON p.ClienteID = c.IdCliente
                    WHERE p.Estado = 'Pendiente'";

        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (m.Numero LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda OR CAST(p.PedidoID AS VARCHAR) LIKE @Busqueda)";
        if (mesaId.HasValue)
            sql += " AND p.MesaID = @MesaID";
        if (empleadoId.HasValue)
            sql += " AND p.EmpleadoID = @EmpleadoID";

        sql += " ORDER BY p.FechaHora DESC";
        sql += " OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

        using var command = new SqlCommand(sql, connection);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");
        if (mesaId.HasValue)
            command.Parameters.AddWithValue("@MesaID", mesaId.Value);
        if (empleadoId.HasValue)
            command.Parameters.AddWithValue("@EmpleadoID", empleadoId.Value);

        command.Parameters.AddWithValue("@Offset", (pagina - 1) * tamPagina);
        command.Parameters.AddWithValue("@Fetch", tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pedidos.Add(MapearPedido(reader));
        }
        return pedidos;
    }

    // SELECT - Cuenta pedidos pendientes de pago con filtros
    public int ContarPedidosPendientes(string? busqueda = null, int? mesaId = null, int? empleadoId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT COUNT(*)
                    FROM Pedidos p
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    WHERE p.Estado = 'Pendiente'";

        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (m.Numero LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda OR CAST(p.PedidoID AS VARCHAR) LIKE @Busqueda)";
        if (mesaId.HasValue)
            sql += " AND p.MesaID = @MesaID";
        if (empleadoId.HasValue)
            sql += " AND p.EmpleadoID = @EmpleadoID";

        using var command = new SqlCommand(sql, connection);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");
        if (mesaId.HasValue)
            command.Parameters.AddWithValue("@MesaID", mesaId.Value);
        if (empleadoId.HasValue)
            command.Parameters.AddWithValue("@EmpleadoID", empleadoId.Value);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Obtiene un pedido por ID con JOINs
    public Pedido? ObtenerPedido(int pedidoId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT p.PedidoID, p.TurnoID, p.ClienteID, p.EmpleadoID, p.MesaID,
                     p.FechaHora, p.TipoServicio, p.Estado, p.Subtotal, p.IGV, p.Total, p.NotasEspeciales,
                     m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado,
                     c.NombreCompleto AS NombreCliente
              FROM Pedidos p
              LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
              INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
              LEFT JOIN Cliente c ON p.ClienteID = c.IdCliente
              WHERE p.PedidoID = @PedidoID", connection);
        command.Parameters.AddWithValue("@PedidoID", pedidoId);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapearPedido(reader);
        }
        return null;
    }

    // SELECT - Lista todas las mesas activas
    public List<Mesa> ObtenerMesas()
    {
        var mesas = new List<Mesa>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT IdMesa, Numero, Estado, Activo FROM Mesa WHERE Activo = 1 ORDER BY Numero", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            mesas.Add(new Mesa
            {
                IdMesa = reader.GetInt32(0),
                Numero = reader.GetInt32(1),
                Estado = reader.GetString(2),
                Activo = reader.GetBoolean(3)
            });
        }
        return mesas;
    }

    // SELECT - Lista empleados activos para dropdowns
    public List<Empleado> ObtenerEmpleados()
    {
        var empleados = new List<Empleado>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT IdEmpleado, NombreCompleto FROM Empleado WHERE Activo = 1 ORDER BY NombreCompleto", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            empleados.Add(new Empleado
            {
                IdEmpleado = reader.GetInt32(0),
                NombreCompleto = reader.GetString(1)
            });
        }
        return empleados;
    }

    // SELECT - Obtiene el número de Yape desde configuración del sistema
    public string? ObtenerNumeroYape()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT NumeroYape FROM ConfiguracionSistema WHERE ConfigID = 1", connection);
        connection.Open();
        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    // SELECT - Verifica si un pedido ya tiene un pago confirmado
    public bool TienePagoConfirmado(int pedidoId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM Pagos WHERE PedidoID = @PedidoID AND Estado = 'Confirmado'", connection);
        command.Parameters.AddWithValue("@PedidoID", pedidoId);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // MAPEO - Convierte un SqlDataReader en objeto Pago
    private Pago MapearPago(SqlDataReader reader)
    {
        return new Pago
        {
            PagoID = reader.GetInt32(0),
            PedidoID = reader.GetInt32(1),
            Metodo = reader.GetString(2),
            Monto = reader.GetDecimal(3),
            Vuelto = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            Fecha = reader.GetDateTime(5),
            Estado = reader.GetString(6),
            QR_Ruta = reader.IsDBNull(7) ? null : reader.GetString(7),
            NumeroMesa = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            NombreEmpleado = reader.GetString(9)
        };
    }

    // MAPEO - Convierte un SqlDataReader en objeto Pedido
    private Pedido MapearPedido(SqlDataReader reader)
    {
        return new Pedido
        {
            PedidoID = reader.GetInt32(0),
            TurnoID = reader.GetInt32(1),
            ClienteID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            EmpleadoID = reader.GetInt32(3),
            MesaID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            FechaHora = reader.GetDateTime(5),
            TipoServicio = reader.GetString(6),
            Estado = reader.GetString(7),
            Subtotal = reader.GetDecimal(8),
            IGV = reader.GetDecimal(9),
            Total = reader.GetDecimal(10),
            NotasEspeciales = reader.IsDBNull(11) ? null : reader.GetString(11),
            NumeroMesa = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
            NombreEmpleado = reader.GetString(13),
            NombreCliente = reader.IsDBNull(14) ? null : reader.GetString(14)
        };
    }

    // Valida si un string es un Base64 válido
    private static bool IsBase64String(string str)
    {
        if (str.Length % 4 != 0) return false;
        try
        {
            Convert.FromBase64String(str);
            return true;
        }
        catch { return false; }
    }
}
