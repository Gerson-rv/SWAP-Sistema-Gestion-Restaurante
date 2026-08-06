using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models.ViewModels;

namespace PRJ_Panaderia.Data;

// Repositorio del Dashboard - Acceso a datos para reportes y estadísticas
public class DashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexion no esta configurada.");
    }

    // SELECT - Obtiene el total de ventas del día actual
    public decimal ObtenerVentasHoy()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT ISNULL(SUM(Total), 0) FROM Pedidos
              WHERE CAST(FechaHora AS DATE) = CAST(GETDATE() AS DATE)
              AND Estado != 'Anulado'", connection);
        connection.Open();
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    // SELECT - Cuenta el número de pedidos del día actual
    public int ObtenerPedidosHoy()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM Pedidos
              WHERE CAST(FechaHora AS DATE) = CAST(GETDATE() AS DATE)
              AND Estado != 'Anulado'", connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Cuenta clientes únicos que atendieron hoy
    public int ObtenerClientesHoy()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COUNT(DISTINCT ClienteID) FROM Pedidos
              WHERE CAST(FechaHora AS DATE) = CAST(GETDATE() AS DATE)
              AND Estado <> 'Anulado'
              AND ClienteID IS NOT NULL", connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Obtiene los últimos pedidos del día
    public List<UltimoPedido> ObtenerUltimosPedidos(int top = 5)
    {
        var lista = new List<UltimoPedido>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT TOP (@Top) p.PedidoID, m.Numero AS NumeroMesa,
                     c.NombreCompleto AS NombreCliente, p.Total, p.Estado, p.TipoServicio
              FROM Pedidos p
              LEFT JOIN Mesa m ON p.MesaID = m.IdMesa
              LEFT JOIN Cliente c ON p.ClienteID = c.IdCliente
              WHERE CAST(p.FechaHora AS DATE) = CAST(GETDATE() AS DATE)
              ORDER BY p.FechaHora DESC", connection);
        command.Parameters.AddWithValue("@Top", top);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new UltimoPedido
            {
                PedidoID = reader.GetInt32(0),
                NumeroMesa = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                NombreCliente = reader.IsDBNull(2) ? null : reader.GetString(2),
                Total = reader.GetDecimal(3),
                Estado = reader.GetString(4),
                TipoServicio = reader.GetString(5)
            });
        }
        return lista;
    }

    // SELECT - Obtiene los productos más vendidos del día
    public List<ProductoTop> ObtenerPlatosTop(int top = 5)
    {
        var lista = new List<ProductoTop>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT TOP (@Top) pr.Nombre, SUM(dp.Cantidad) AS Total
              FROM DetallePedido dp
              INNER JOIN Pedidos p ON dp.PedidoID = p.PedidoID
              INNER JOIN Producto pr ON dp.ProductoID = pr.IdProducto
              WHERE CAST(p.FechaHora AS DATE) = CAST(GETDATE() AS DATE)
              GROUP BY pr.Nombre
              ORDER BY Total DESC", connection);
        command.Parameters.AddWithValue("@Top", top);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new ProductoTop
            {
                Nombre = reader.GetString(0),
                Cantidad = reader.GetInt32(1)
            });
        }
        return lista;
    }

    // SELECT - Obtiene ventas de los últimos 7 días para gráfico
    public List<VentasDia> ObtenerVentas7Dias()
    {
        var lista = new List<VentasDia>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT CAST(FechaHora AS DATE) AS Fecha, ISNULL(SUM(Total), 0) AS Total
              FROM Pedidos
              WHERE FechaHora >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
              AND Estado != 'Anulado'
              GROUP BY CAST(FechaHora AS DATE)
              ORDER BY Fecha", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var fecha = reader.GetDateTime(0);
            lista.Add(new VentasDia
            {
                Fecha = fecha.ToString("dd/MM"),
                DiaAbrev = fecha.ToString("ddd", new System.Globalization.CultureInfo("es-PE")),
                Total = reader.GetDecimal(1)
            });
        }
        return lista;
    }

    // SELECT - Obtiene estado de todas las mesas con empleado asignado
    public List<MesaEstado> ObtenerMesas()
    {
        var lista = new List<MesaEstado>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT m.IdMesa, m.Numero, m.Estado,
                     e.NombreCompleto AS NombreEmpleado
              FROM Mesa m
              OUTER APPLY (
                  SELECT TOP 1 p.EmpleadoID FROM Pedidos p
                  WHERE p.MesaID = m.IdMesa AND p.Estado = 'Pendiente'
                  ORDER BY p.FechaHora DESC
              ) pe
              LEFT JOIN Empleado e ON pe.EmpleadoID = e.IdEmpleado
              WHERE m.Activo = 1
              ORDER BY m.Numero", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new MesaEstado
            {
                IdMesa = reader.GetInt32(0),
                Numero = reader.GetInt32(1),
                Estado = reader.GetString(2),
                NombreEmpleado = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return lista;
    }
}
