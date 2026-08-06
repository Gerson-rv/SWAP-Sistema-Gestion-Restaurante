using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Detalles de Pedido - Acceso a datos de tabla DetallePedido
public class DetallePedidoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public DetallePedidoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista pedidos con paginación y filtros
    public List<Pedido> Listar(int? mesaId = null, string? estadoEntrega = null, string? estadoPago = null, string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var pedidos = new List<Pedido>();
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT p.PedidoID, p.TurnoID, p.ClienteID, p.EmpleadoID, p.MesaID,
                           p.FechaHora, p.TipoServicio, p.Estado, p.Subtotal, p.IGV, p.Total, p.NotasEspeciales,
                           m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado,
                           c.NombreCompleto AS NombreCliente,
                           CASE 
                             WHEN NOT EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID) THEN 'Pendiente'
                             WHEN EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0) THEN 'Pendiente'
                             ELSE 'Servido'
                           END AS EstadoEntrega
                    FROM Pedidos p
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    LEFT JOIN Cliente c ON p.ClienteID = c.IdCliente
                    WHERE 1=1";

        if (mesaId.HasValue)
            sql += " AND p.MesaID = @MesaID";
        if (!string.IsNullOrEmpty(estadoEntrega))
        {
            if (estadoEntrega == "Pendiente")
                sql += " AND EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0)";
            else if (estadoEntrega == "Servido")
                sql += " AND NOT EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0)";
        }
        if (!string.IsNullOrEmpty(estadoPago))
            sql += " AND p.Estado = @EstadoPago";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (m.Numero LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda OR CAST(p.PedidoID AS VARCHAR) LIKE @Busqueda)";

        sql += " ORDER BY p.FechaHora DESC";
        sql += " OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

        using var command = new SqlCommand(sql, connection);
        if (mesaId.HasValue)
            command.Parameters.AddWithValue("@MesaID", mesaId.Value);
        if (!string.IsNullOrEmpty(estadoEntrega))
            command.Parameters.AddWithValue("@EstadoEntrega", estadoEntrega);
        if (!string.IsNullOrEmpty(estadoPago))
            command.Parameters.AddWithValue("@EstadoPago", estadoPago);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

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

    // SELECT - Cuenta total de pedidos con filtros para paginación
    public int Contar(int? mesaId = null, string? estadoEntrega = null, string? estadoPago = null, string? busqueda = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT COUNT(*)
                    FROM Pedidos p
                    LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
                    INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
                    WHERE 1=1";

        if (mesaId.HasValue)
            sql += " AND p.MesaID = @MesaID";
        if (!string.IsNullOrEmpty(estadoEntrega))
        {
            if (estadoEntrega == "Pendiente")
                sql += " AND EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0)";
            else if (estadoEntrega == "Servido")
                sql += " AND NOT EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0)";
        }
        if (!string.IsNullOrEmpty(estadoPago))
            sql += " AND p.Estado = @EstadoPago";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (m.Numero LIKE @Busqueda OR e.NombreCompleto LIKE @Busqueda OR CAST(p.PedidoID AS VARCHAR) LIKE @Busqueda)";

        using var command = new SqlCommand(sql, connection);
        if (mesaId.HasValue)
            command.Parameters.AddWithValue("@MesaID", mesaId.Value);
        if (!string.IsNullOrEmpty(estadoEntrega))
            command.Parameters.AddWithValue("@EstadoEntrega", estadoEntrega);
        if (!string.IsNullOrEmpty(estadoPago))
            command.Parameters.AddWithValue("@EstadoPago", estadoPago);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Obtiene un pedido con todos sus detalles
    public (Pedido? pedido, List<DetallePedido> detalles) ObtenerConDetalles(int pedidoId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        Pedido? pedido = null;
        using (var cmdPedido = new SqlCommand(
            @"SELECT p.PedidoID, p.TurnoID, p.ClienteID, p.EmpleadoID, p.MesaID,
                     p.FechaHora, p.TipoServicio, p.Estado, p.Subtotal, p.IGV, p.Total, p.NotasEspeciales,
                     m.Numero AS NumeroMesa, e.NombreCompleto AS NombreEmpleado,
                     c.NombreCompleto AS NombreCliente,
                     CASE 
                       WHEN NOT EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID) THEN 'Pendiente'
                       WHEN EXISTS (SELECT 1 FROM DetallePedido dp WHERE dp.PedidoID = p.PedidoID AND dp.Entregado = 0) THEN 'Pendiente'
                       ELSE 'Servido'
                     END AS EstadoEntrega
              FROM Pedidos p
              LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
              INNER JOIN Empleado e ON p.EmpleadoID = e.IdEmpleado
              LEFT JOIN Cliente c ON p.ClienteID = c.IdCliente
              WHERE p.PedidoID = @PedidoID", connection))
        {
            cmdPedido.Parameters.AddWithValue("@PedidoID", pedidoId);
            using var readerPedido = cmdPedido.ExecuteReader();
            if (readerPedido.Read())
            {
                pedido = MapearPedido(readerPedido);
            }
        }

        var detalles = new List<DetallePedido>();
        using (var cmdDetalles = new SqlCommand(
            @"SELECT dp.DetalleID, dp.PedidoID, dp.ProductoID, dp.Cantidad, dp.PrecioUnitario,
                     dp.Modificadores, dp.Entregado, pr.Nombre AS NombreProducto
              FROM DetallePedido dp
              INNER JOIN Producto pr ON dp.ProductoID = pr.IdProducto
              WHERE dp.PedidoID = @PedidoID
              ORDER BY dp.DetalleID", connection))
        {
            cmdDetalles.Parameters.AddWithValue("@PedidoID", pedidoId);
            using var readerDetalles = cmdDetalles.ExecuteReader();
            while (readerDetalles.Read())
            {
                detalles.Add(new DetallePedido
                {
                    DetalleID = readerDetalles.GetInt32(0),
                    PedidoID = readerDetalles.GetInt32(1),
                    ProductoID = readerDetalles.GetInt32(2),
                    Cantidad = readerDetalles.GetInt32(3),
                    PrecioUnitario = readerDetalles.GetDecimal(4),
                    Modificadores = readerDetalles.IsDBNull(5) ? null : readerDetalles.GetString(5),
                    Entregado = readerDetalles.GetBoolean(6),
                    NombreProducto = readerDetalles.GetString(7)
                });
            }
        }

        return (pedido, detalles);
    }

    // INSERT - Crea un detalle de pedido y retorna el ID generado
    public int CrearDetalle(DetallePedido detalle, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO DetallePedido (PedidoID, ProductoID, Cantidad, PrecioUnitario, Modificadores, Entregado)
              VALUES (@PedidoID, @ProductoID, @Cantidad, @PrecioUnitario, @Modificadores, @Entregado);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@PedidoID", detalle.PedidoID);
        command.Parameters.AddWithValue("@ProductoID", detalle.ProductoID);
        command.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
        command.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
        command.Parameters.AddWithValue("@Modificadores", (object?)detalle.Modificadores ?? DBNull.Value);
        command.Parameters.AddWithValue("@Entregado", detalle.Entregado);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("DetallePedido", id, "INSERT", empleadoId,
            $"Detalle PedidoID {detalle.PedidoID}: x{detalle.Cantidad} ProductoID {detalle.ProductoID}");
        return id;
    }

    // UPDATE - Actualiza un detalle de pedido existente
    public void ActualizarDetalle(DetallePedido detalle, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE DetallePedido
              SET ProductoID = @ProductoID, Cantidad = @Cantidad, PrecioUnitario = @PrecioUnitario,
                  Modificadores = @Modificadores, Entregado = @Entregado
              WHERE DetalleID = @DetalleID", connection);
        command.Parameters.AddWithValue("@DetalleID", detalle.DetalleID);
        command.Parameters.AddWithValue("@ProductoID", detalle.ProductoID);
        command.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
        command.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
        command.Parameters.AddWithValue("@Modificadores", (object?)detalle.Modificadores ?? DBNull.Value);
        command.Parameters.AddWithValue("@Entregado", detalle.Entregado);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("DetallePedido", detalle.DetalleID, "UPDATE", empleadoId,
            $"Detalle ID {detalle.DetalleID}: cantidad x{detalle.Cantidad}, entregado={detalle.Entregado}");
    }

    // DELETE - Elimina un detalle de pedido por ID
    public void EliminarDetalle(int detalleId, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("DELETE FROM DetallePedido WHERE DetalleID = @Id", connection);
        command.Parameters.AddWithValue("@Id", detalleId);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("DetallePedido", detalleId, "DELETE", empleadoId,
            $"Detalle ID {detalleId} eliminado");
    }

    // UPDATE - Marca todos los detalles de un pedido como entregados
    public void MarcarComoServido(int pedidoId, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "UPDATE DetallePedido SET Entregado = 1 WHERE PedidoID = @PedidoID", connection);
        command.Parameters.AddWithValue("@PedidoID", pedidoId);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("DetallePedido", pedidoId, "UPDATE", empleadoId,
            $"Todos los detalles del Pedido C-{pedidoId:D4} marcados como entregados");
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

    // SELECT - Lista productos activos para selección
    public List<Producto> ObtenerProductos()
    {
        var productos = new List<Producto>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT IdProducto, Nombre, Precio FROM Producto WHERE Activo = 1 ORDER BY Nombre", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(new Producto
            {
                IdProducto = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Precio = reader.GetDecimal(2)
            });
        }
        return productos;
    }

    // SELECT - Lista todos los pedidos ordenados por ID descendente
    public List<Pedido> ObtenerPedidos()
    {
        var pedidos = new List<Pedido>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT PedidoID, TurnoID, EmpleadoID, MesaID, FechaHora, TipoServicio, Estado, Subtotal, IGV, Total
              FROM Pedidos ORDER BY PedidoID DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pedidos.Add(new Pedido
            {
                PedidoID = reader.GetInt32(0),
                TurnoID = reader.GetInt32(1),
                EmpleadoID = reader.GetInt32(2),
                MesaID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                FechaHora = reader.GetDateTime(4),
                TipoServicio = reader.GetString(5),
                Estado = reader.GetString(6),
                Subtotal = reader.GetDecimal(7),
                IGV = reader.GetDecimal(8),
                Total = reader.GetDecimal(9)
            });
        }
        return pedidos;
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
            NombreCliente = reader.IsDBNull(14) ? null : reader.GetString(14),
            EstadoEntrega = reader.IsDBNull(15) ? "Pendiente" : reader.GetString(15)
        };
    }
}
