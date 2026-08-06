using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Pedidos - Acceso a datos de tabla Pedidos
public class PedidoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public PedidoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista pedidos con filtros opcionales (mesa, estado, fechas)
    public List<Pedido> Listar(int? mesaId = null, string? estado = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
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
                    WHERE 1=1";

        if (mesaId.HasValue)
        {
            sql += " AND p.MesaID = @MesaID";
        }
        if (!string.IsNullOrEmpty(estado))
        {
            sql += " AND p.Estado = @Estado";
        }
        if (fechaInicio.HasValue)
        {
            sql += " AND p.FechaHora >= @FechaInicio";
        }
        if (fechaFin.HasValue)
        {
            sql += " AND p.FechaHora <= @FechaFin";
        }
        sql += " ORDER BY p.FechaHora DESC";

        using var command = new SqlCommand(sql, connection);
        if (mesaId.HasValue)
            command.Parameters.AddWithValue("@MesaID", mesaId.Value);
        if (!string.IsNullOrEmpty(estado))
            command.Parameters.AddWithValue("@Estado", estado);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pedidos.Add(Mapear(reader));
        }
        return pedidos;
    }

    // SELECT - Obtiene un pedido por ID con JOINs a Mesa, Empleado y Cliente
    public Pedido? ObtenerPorId(int id)
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
              WHERE p.PedidoID = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // SELECT - Obtiene los detalles de un pedido específico
    public List<DetallePedido> ObtenerDetalles(int pedidoId)
    {
        var detalles = new List<DetallePedido>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT dp.DetalleID, dp.PedidoID, dp.ProductoID, dp.Cantidad, dp.PrecioUnitario, dp.Modificadores,
                     pr.Nombre AS NombreProducto
              FROM DetallePedido dp
              INNER JOIN Producto pr ON dp.ProductoID = pr.IdProducto
              WHERE dp.PedidoID = @PedidoID
              ORDER BY dp.DetalleID", connection);
        command.Parameters.AddWithValue("@PedidoID", pedidoId);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            detalles.Add(new DetallePedido
            {
                DetalleID = reader.GetInt32(0),
                PedidoID = reader.GetInt32(1),
                ProductoID = reader.GetInt32(2),
                Cantidad = reader.GetInt32(3),
                PrecioUnitario = reader.GetDecimal(4),
                Modificadores = reader.IsDBNull(5) ? null : reader.GetString(5),
                NombreProducto = reader.GetString(6)
            });
        }
        return detalles;
    }

    // INSERT - Crea un pedido con sus detalles en transacción
    public int CrearPedido(Pedido pedido, List<DetallePedido> detalles)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmdPedido = new SqlCommand(
                @"INSERT INTO Pedidos (TurnoID, ClienteID, EmpleadoID, MesaID, TipoServicio, Estado, Subtotal, IGV, Total, NotasEspeciales)
                  VALUES (@TurnoID, @ClienteID, @EmpleadoID, @MesaID, @TipoServicio, @Estado, @Subtotal, @IGV, @Total, @NotasEspeciales);
                  SELECT SCOPE_IDENTITY();", connection, transaction);

            cmdPedido.Parameters.AddWithValue("@TurnoID", pedido.TurnoID);
            cmdPedido.Parameters.AddWithValue("@ClienteID", (object?)pedido.ClienteID ?? DBNull.Value);
            cmdPedido.Parameters.AddWithValue("@EmpleadoID", pedido.EmpleadoID);
            cmdPedido.Parameters.AddWithValue("@MesaID", (object?)pedido.MesaID ?? DBNull.Value);
            cmdPedido.Parameters.AddWithValue("@TipoServicio", pedido.TipoServicio);
            cmdPedido.Parameters.AddWithValue("@Estado", pedido.Estado);
            cmdPedido.Parameters.AddWithValue("@Subtotal", pedido.Subtotal);
            cmdPedido.Parameters.AddWithValue("@IGV", pedido.IGV);
            cmdPedido.Parameters.AddWithValue("@Total", pedido.Total);
            cmdPedido.Parameters.AddWithValue("@NotasEspeciales", (object?)pedido.NotasEspeciales ?? DBNull.Value);

            var pedidoId = Convert.ToInt32(cmdPedido.ExecuteScalar());

            foreach (var detalle in detalles)
            {
                using var cmdDetalle = new SqlCommand(
                    @"INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario, Modificadores)
                      VALUES (@PedidoID, @ProductoID, @Cantidad, @PrecioUnitario, @Modificadores)", connection, transaction);

                cmdDetalle.Parameters.AddWithValue("@PedidoID", pedidoId);
                cmdDetalle.Parameters.AddWithValue("@ProductoID", detalle.ProductoID);
                cmdDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                cmdDetalle.Parameters.AddWithValue("@Modificadores", (object?)detalle.Modificadores ?? DBNull.Value);
                cmdDetalle.ExecuteNonQuery();
            }

            if (pedido.MesaID.HasValue)
            {
                using var cmdMesa = new SqlCommand(
                    @"UPDATE Mesa SET Estado = 'Ocupada' WHERE IdMesa = @MesaID", connection, transaction);
                cmdMesa.Parameters.AddWithValue("@MesaID", pedido.MesaID.Value);
                cmdMesa.ExecuteNonQuery();
            }

            transaction.Commit();
            _auditRepo.Registrar("Pedidos", pedidoId, "INSERT", pedido.EmpleadoID,
                $"Pedido C-{pedidoId:D4} creado ({pedido.TipoServicio})");
            return pedidoId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // UPDATE - Cambia el estado de un pedido
    public void CambiarEstado(int id, string estado, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Pedidos SET Estado = @Estado WHERE PedidoID = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Estado", estado);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Pedidos", id, "UPDATE", empleadoId,
            $"Pedido C-{id:D4} cambio a {estado}");
    }

    // UPDATE - Marca un pedido como pagado y libera la mesa
    public void MarcarPagado(int pedidoId, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmdPedido = new SqlCommand(
                @"UPDATE Pedidos SET Estado = 'Pagado' WHERE PedidoID = @PedidoID", connection, transaction);
            cmdPedido.Parameters.AddWithValue("@PedidoID", pedidoId);
            cmdPedido.ExecuteNonQuery();

            using var cmdMesa = new SqlCommand(
                @"UPDATE Mesa SET Estado = 'Libre'
                  WHERE IdMesa = (SELECT MesaID FROM Pedidos WHERE PedidoID = @PedidoID AND MesaID IS NOT NULL)", connection, transaction);
            cmdMesa.Parameters.AddWithValue("@PedidoID", pedidoId);
            cmdMesa.ExecuteNonQuery();

            transaction.Commit();
            _auditRepo.Registrar("Pedidos", pedidoId, "UPDATE", empleadoId,
                $"Pedido C-{pedidoId:D4} marcado como Pagado");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // UPDATE - Anula un pedido pendiente y libera la mesa
    public void Anular(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var cmdPedido = new SqlCommand(
                @"UPDATE Pedidos SET Estado = 'Anulado' WHERE PedidoID = @Id AND Estado = 'Pendiente'", connection, transaction);
            cmdPedido.Parameters.AddWithValue("@Id", id);
            var affected = cmdPedido.ExecuteNonQuery();

            if (affected > 0)
            {
                using var cmdMesa = new SqlCommand(
                    @"UPDATE Mesa SET Estado = 'Libre'
                      WHERE IdMesa = (SELECT MesaID FROM Pedidos WHERE PedidoID = @Id AND MesaID IS NOT NULL)", connection, transaction);
                cmdMesa.Parameters.AddWithValue("@Id", id);
                cmdMesa.ExecuteNonQuery();
            }

            transaction.Commit();
            if (affected > 0)
                _auditRepo.Registrar("Pedidos", id, "ANULAR", empleadoId,
                    $"Pedido C-{id:D4} anulado");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // SELECT - Obtiene el siguiente ID de pedido disponible
    public int ObtenerSiguientePedidoId()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT ISNULL(MAX(PedidoID), 0) + 1 FROM Pedidos", connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Lista productos activos con filtro opcional de categoría
    public List<Producto> ObtenerProductosActivos(int? idCategoria = null)
    {
        var productos = new List<Producto>();
        using var connection = new SqlConnection(_connectionString);
        var sql = @"SELECT p.IdProducto, p.IdCategoria, p.Nombre, p.Precio,
                           p.RutaImagen, p.Activo, p.FechaCreacion, c.Nombre AS NombreCategoria
                    FROM Producto p
                    INNER JOIN Categoria c ON p.IdCategoria = c.IdCategoria
                    WHERE p.Activo = 1";

        if (idCategoria.HasValue)
        {
            sql += " AND p.IdCategoria = @IdCategoria";
        }
        sql += " ORDER BY p.Nombre";

        using var command = new SqlCommand(sql, connection);
        if (idCategoria.HasValue)
            command.Parameters.AddWithValue("@IdCategoria", idCategoria.Value);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(new Producto
            {
                IdProducto = reader.GetInt32(0),
                IdCategoria = reader.GetInt32(1),
                Nombre = reader.GetString(2),
                Precio = reader.GetDecimal(3),
                RutaImagen = reader.IsDBNull(4) ? null : reader.GetString(4),
                Activo = reader.GetBoolean(5),
                FechaCreacion = reader.GetDateTime(6),
                NombreCategoria = reader.GetString(7)
            });
        }
        return productos;
    }

    // SELECT - Lista las categorías activas para dropdowns
    public List<Categoria> ObtenerCategoriasActivas()
    {
        var categorias = new List<Categoria>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdCategoria, Nombre FROM Categoria WHERE Activo = 1 ORDER BY Nombre", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categorias.Add(new Categoria
            {
                IdCategoria = reader.GetInt32(0),
                Nombre = reader.GetString(1)
            });
        }
        return categorias;
    }

    // SELECT - Lista mesas libres y activas
    public List<Mesa> ObtenerMesasDisponibles()
    {
        var mesas = new List<Mesa>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdMesa, Numero, Estado, Activo
              FROM Mesa
              WHERE Activo = 1 AND Estado = 'Libre'
              ORDER BY Numero ASC", connection);
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
    public List<Empleado> ObtenerEmpleadosActivos()
    {
        var empleados = new List<Empleado>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT e.IdEmpleado, e.NombreCompleto
              FROM Empleado e
              WHERE e.Activo = 1
              ORDER BY e.NombreCompleto", connection);
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

    // SELECT - Obtiene el IdEmpleado por su usuario de login
    public int? ObtenerEmpleadoIdPorUsuario(string usuario)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT IdEmpleado FROM Empleado WHERE Usuario = @Usuario AND Activo = 1", connection);
        command.Parameters.AddWithValue("@Usuario", usuario);
        connection.Open();
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : null;
    }

    // SELECT - Verifica si todos los detalles de un pedido fueron entregados
    public bool EstaCompletoEntregado(int pedidoId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT CASE WHEN NOT EXISTS (
                SELECT 1 FROM DetallePedido WHERE PedidoID = @PedidoID AND Entregado = 0
              ) THEN 1 ELSE 0 END", connection);
        command.Parameters.AddWithValue("@PedidoID", pedidoId);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    // SELECT - Verifica si una mesa tiene un pedido pendiente activo
    public bool TienePedidoPendienteEnMesa(int mesaId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM Pedidos WHERE MesaID = @MesaID AND Estado = 'Pendiente'", connection);
        command.Parameters.AddWithValue("@MesaID", mesaId);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // SELECT - Verifica si un turno existe y está abierto
    public bool TurnoEstaAbierto(int turnoId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM TurnosCaja WHERE IdTurno = @TurnoID AND FechaCierre IS NULL", connection);
        command.Parameters.AddWithValue("@TurnoID", turnoId);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // SELECT - Obtiene el turno de caja actualmente abierto
    public TurnoCaja? ObtenerTurnoAbierto()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT TOP 1 t.IdTurno, t.IdEmpleado, t.FechaApertura, t.FechaCierre,
                     t.MontoInicial, t.MontoCierre, t.Observaciones,
                     e.NombreCompleto AS NombreEmpleado
              FROM TurnosCaja t
              INNER JOIN Empleado e ON t.IdEmpleado = e.IdEmpleado
              WHERE t.FechaCierre IS NULL
              ORDER BY t.FechaApertura DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new TurnoCaja
            {
                IdTurno = reader.GetInt32(0),
                IdEmpleado = reader.GetInt32(1),
                FechaApertura = reader.GetDateTime(2),
                FechaCierre = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                MontoInicial = reader.GetDecimal(4),
                MontoCierre = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                Observaciones = reader.IsDBNull(6) ? null : reader.GetString(6),
                NombreEmpleado = reader.GetString(7)
            };
        }
        return null;
    }

    // MAPEO - Convierte un SqlDataReader en objeto Pedido
    private Pedido Mapear(SqlDataReader reader)
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
}
