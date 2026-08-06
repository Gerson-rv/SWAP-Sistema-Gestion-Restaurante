using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Clientes - Acceso a datos de tabla Cliente
public class ClienteRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public ClienteRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todos los clientes ordenados por ID descendente
    public List<Cliente> Listar()
    {
        var clientes = new List<Cliente>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdCliente, Dni, NombreCompleto, Telefono, FechaRegistro, Activo
              FROM Cliente ORDER BY IdCliente DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            clientes.Add(Mapear(reader));
        }
        return clientes;
    }

    // SELECT - Obtiene un cliente por su ID
    public Cliente? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdCliente, Dni, NombreCompleto, Telefono, FechaRegistro, Activo
              FROM Cliente WHERE IdCliente = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // INSERT - Crea un nuevo cliente y retorna el ID generado
    public int Crear(Cliente cliente, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO Cliente (Dni, NombreCompleto, Telefono, Activo)
              VALUES (@Dni, @NombreCompleto, @Telefono, @Activo);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@Dni", cliente.Dni);
        command.Parameters.AddWithValue("@NombreCompleto", cliente.NombreCompleto);
        command.Parameters.AddWithValue("@Telefono", (object?)cliente.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", cliente.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Cliente", id, "INSERT", empleadoId,
            $"Cliente '{cliente.NombreCompleto}' (DNI: {cliente.Dni}) creado");
        return id;
    }

    // UPDATE - Actualiza los datos de un cliente existente
    public void Actualizar(Cliente cliente, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Cliente SET Dni = @Dni, NombreCompleto = @NombreCompleto,
                     Telefono = @Telefono, Activo = @Activo
              WHERE IdCliente = @Id", connection);
        command.Parameters.AddWithValue("@Id", cliente.IdCliente);
        command.Parameters.AddWithValue("@Dni", cliente.Dni);
        command.Parameters.AddWithValue("@NombreCompleto", cliente.NombreCompleto);
        command.Parameters.AddWithValue("@Telefono", (object?)cliente.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", cliente.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Cliente", cliente.IdCliente, "UPDATE", empleadoId,
            $"Cliente '{cliente.NombreCompleto}' actualizado - Estado: {(cliente.Activo ? "Activo" : "Inactivo")}");
    }

    // DELETE - Elimina un cliente por su ID
    public void Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("DELETE FROM Cliente WHERE IdCliente = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Cliente", id, "DELETE", empleadoId,
            $"Cliente ID {id} eliminado");
    }

    // MAPEO - Convierte un SqlDataReader en objeto Cliente
    private Cliente Mapear(SqlDataReader reader)
    {
        return new Cliente
        {
            IdCliente = reader.GetInt32(0),
            Dni = reader.GetString(1),
            NombreCompleto = reader.GetString(2),
            Telefono = reader.IsDBNull(3) ? null : reader.GetString(3),
            FechaRegistro = reader.GetDateTime(4),
            Activo = reader.GetBoolean(5)
        };
    }
}
