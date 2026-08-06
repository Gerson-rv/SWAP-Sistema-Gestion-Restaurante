using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Productos - Acceso a datos de tabla Producto
public class ProductoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public ProductoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todos los productos con INNER JOIN a Categoría
    public List<Producto> Listar()
    {
        var productos = new List<Producto>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT p.IdProducto, p.IdCategoria, p.Nombre, p.Precio,
                     p.RutaImagen, p.Activo, p.FechaCreacion,
                     c.Nombre AS NombreCategoria
              FROM Producto p
              INNER JOIN Categoria c ON p.IdCategoria = c.IdCategoria
              ORDER BY p.IdProducto DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(Mapear(reader));
        }
        return productos;
    }

    // SELECT - Obtiene un producto por su ID con JOIN a Categoría
    public Producto? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT p.IdProducto, p.IdCategoria, p.Nombre, p.Precio,
                     p.RutaImagen, p.Activo, p.FechaCreacion,
                     c.Nombre AS NombreCategoria
              FROM Producto p
              INNER JOIN Categoria c ON p.IdCategoria = c.IdCategoria
              WHERE p.IdProducto = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // INSERT - Crea un nuevo producto y retorna el ID generado
    public int Crear(Producto producto, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO Producto (IdCategoria, Nombre, Precio, RutaImagen, Activo)
              VALUES (@IdCategoria, @Nombre, @Precio, @RutaImagen, @Activo);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@IdCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("@Nombre", producto.Nombre);
        command.Parameters.AddWithValue("@Precio", producto.Precio);
        command.Parameters.AddWithValue("@RutaImagen", (object?)producto.RutaImagen ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", producto.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Producto", id, "INSERT", empleadoId,
            $"Producto '{producto.Nombre}' (S/{producto.Precio:N2}) creado");
        return id;
    }

    // UPDATE - Actualiza los datos de un producto existente
    public void Actualizar(Producto producto, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Producto SET IdCategoria = @IdCategoria, Nombre = @Nombre,
                     Precio = @Precio, RutaImagen = @RutaImagen, Activo = @Activo
              WHERE IdProducto = @Id", connection);
        command.Parameters.AddWithValue("@Id", producto.IdProducto);
        command.Parameters.AddWithValue("@IdCategoria", producto.IdCategoria);
        command.Parameters.AddWithValue("@Nombre", producto.Nombre);
        command.Parameters.AddWithValue("@Precio", producto.Precio);
        command.Parameters.AddWithValue("@RutaImagen", (object?)producto.RutaImagen ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", producto.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Producto", producto.IdProducto, "UPDATE", empleadoId,
            $"Producto '{producto.Nombre}' actualizado - Estado: {(producto.Activo ? "Activo" : "Inactivo")}");
    }

    // DELETE - Elimina un producto por su ID
    public void Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("DELETE FROM Producto WHERE IdProducto = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Producto", id, "DELETE", empleadoId,
            $"Producto ID {id} eliminado");
    }

    // SELECT - Lista las categorías activas para dropdowns
    public List<Categoria> ObtenerCategoriasActivas()
    {
        var categorias = new List<Categoria>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCategoria, Nombre FROM Categoria WHERE Activo = 1 ORDER BY Nombre", connection);
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

    // MAPEO - Convierte un SqlDataReader en objeto Producto
    private Producto Mapear(SqlDataReader reader)
    {
        return new Producto
        {
            IdProducto = reader.GetInt32(0),
            IdCategoria = reader.GetInt32(1),
            Nombre = reader.GetString(2),
            Precio = reader.GetDecimal(3),
            RutaImagen = reader.IsDBNull(4) ? null : reader.GetString(4),
            Activo = reader.GetBoolean(5),
            FechaCreacion = reader.GetDateTime(6),
            NombreCategoria = reader.GetString(7)
        };
    }

    // SELECT - Cuenta detalles de pedido que usan un producto
    public int ContarDetallesPedido(int idProducto)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT COUNT(1) FROM DetallePedido WHERE ProductoID = @IdProducto", connection);
        command.Parameters.AddWithValue("@IdProducto", idProducto);
        connection.Open();
        return (int)command.ExecuteScalar();
    }
}
