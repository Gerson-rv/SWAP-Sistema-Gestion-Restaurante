using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Categorías - Acceso a datos de tabla Categoria
public class CategoriaRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public CategoriaRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todas las categorías ordenadas por ID descendente
    public List<Categoria> Listar()
    {
        var categorias = new List<Categoria>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCategoria, Nombre, Activo FROM Categoria ORDER BY IdCategoria DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categorias.Add(new Categoria
            {
                IdCategoria = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Activo = reader.GetBoolean(2)
            });
        }
        return categorias;
    }

    // SELECT - Obtiene una categoría por su ID
    public Categoria? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCategoria, Nombre, Activo FROM Categoria WHERE IdCategoria = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Categoria
            {
                IdCategoria = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Activo = reader.GetBoolean(2)
            };
        }
        return null;
    }

    // INSERT - Crea una nueva categoría y retorna el ID generado
    public int Crear(Categoria categoria, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "INSERT INTO Categoria (Nombre, Activo) VALUES (@Nombre, @Activo); SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
        command.Parameters.AddWithValue("@Activo", categoria.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Categoria", id, "INSERT", empleadoId,
            $"Categoria '{categoria.Nombre}' creada");
        return id;
    }

    // UPDATE - Actualiza los datos de una categoría existente
    public void Actualizar(Categoria categoria, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "UPDATE Categoria SET Nombre = @Nombre, Activo = @Activo WHERE IdCategoria = @Id", connection);
        command.Parameters.AddWithValue("@Id", categoria.IdCategoria);
        command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
        command.Parameters.AddWithValue("@Activo", categoria.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Categoria", categoria.IdCategoria, "UPDATE", empleadoId,
            $"Categoria '{categoria.Nombre}' actualizada - Estado: {(categoria.Activo ? "Activa" : "Inactiva")}");
    }

    // DELETE - Elimina una categoría por su ID
    public void Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "DELETE FROM Categoria WHERE IdCategoria = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", id);

        connection.Open();
        command.ExecuteNonQuery();

        _auditRepo.Registrar(
            "Categoria",
            id,
            "DELETE",
            empleadoId,
            $"Categoria ID {id} eliminada");
    }
}
