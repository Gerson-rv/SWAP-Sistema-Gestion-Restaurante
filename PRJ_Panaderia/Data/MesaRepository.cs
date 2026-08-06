using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Mesas - Acceso a datos de tabla Mesa
public class MesaRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public MesaRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todas las mesas ordenadas por número ascendente
    public List<Mesa> Listar()
    {
        var mesas = new List<Mesa>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdMesa, Numero, Estado, Activo
              FROM Mesa ORDER BY Numero ASC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            mesas.Add(Mapear(reader));
        }
        return mesas;
    }

    // SELECT - Lista mesas filtradas por estado (Libre/Ocupada)
    public List<Mesa> ListarPorEstado(string? estado)
    {
        var mesas = new List<Mesa>();
        using var connection = new SqlConnection(_connectionString);
        var sql = @"SELECT IdMesa, Numero, Estado, Activo
                    FROM Mesa";
        if (!string.IsNullOrEmpty(estado))
        {
            sql += " WHERE Estado = @Estado";
        }
        sql += " ORDER BY Numero ASC";
        using var command = new SqlCommand(sql, connection);
        if (!string.IsNullOrEmpty(estado))
        {
            command.Parameters.AddWithValue("@Estado", estado);
        }
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            mesas.Add(Mapear(reader));
        }
        return mesas;
    }

    // SELECT - Obtiene una mesa por su ID
    public Mesa? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT IdMesa, Numero, Estado, Activo
              FROM Mesa WHERE IdMesa = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // INSERT - Crea una nueva mesa y retorna el ID generado
    public int Crear(Mesa mesa, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO Mesa (Numero, Estado, Activo)
              VALUES (@Numero, @Estado, @Activo);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@Numero", mesa.Numero);
        command.Parameters.AddWithValue("@Estado", mesa.Estado);
        command.Parameters.AddWithValue("@Activo", mesa.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Mesa", id, "INSERT", empleadoId,
            $"Mesa #{mesa.Numero} creada (Estado: {mesa.Estado})");
        return id;
    }

    // UPDATE - Actualiza los datos de una mesa existente
    public void Actualizar(Mesa mesa, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Mesa SET Numero = @Numero, Estado = @Estado, Activo = @Activo
              WHERE IdMesa = @Id", connection);
        command.Parameters.AddWithValue("@Id", mesa.IdMesa);
        command.Parameters.AddWithValue("@Numero", mesa.Numero);
        command.Parameters.AddWithValue("@Estado", mesa.Estado);
        command.Parameters.AddWithValue("@Activo", mesa.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Mesa", mesa.IdMesa, "UPDATE", empleadoId,
            $"Mesa #{mesa.Numero} actualizada - Estado: {(mesa.Activo ? "Activa" : "Inactiva")}");
    }

    // UPDATE - Activa o desactiva una mesa
    public void CambiarEstado(int id, bool activo, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Mesa SET Activo = @Activo WHERE IdMesa = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Activo", activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Mesa", id, "UPDATE", empleadoId,
            $"Mesa ID {id} {(activo ? "activada" : "desactivada")}");
    }

    // DELETE - Elimina una mesa por su ID (solo si está deshabilitada)
    public bool Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Verificar el estado actual de la mesa
        using (var verificar = new SqlCommand(
            "SELECT Estado FROM Mesa WHERE IdMesa = @Id", connection))
        {
            verificar.Parameters.AddWithValue("@Id", id);
            var estado = verificar.ExecuteScalar()?.ToString();

            // Si la mesa no existe
            if (estado == null)
                return false;

            // Solo permitir eliminar si está deshabilitada
            if (estado != "Deshabilitada")
                return false;
        }

        // Ejecutar la eliminación
        using var command = new SqlCommand(
            "DELETE FROM Mesa WHERE IdMesa = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        int filas = command.ExecuteNonQuery();

        if (filas > 0)
        {
            _auditRepo.Registrar("Mesa", id, "DELETE", empleadoId,
                $"Mesa ID {id} eliminada");
            return true;
        }

        return false;
    }

    // SELECT - Verifica si ya existe una mesa con ese número
    public bool ExisteNumero(int numero, int? excludeId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM Mesa WHERE Numero = @Numero";
        if (excludeId.HasValue)
        {
            sql += " AND IdMesa != @Id";
        }
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Numero", numero);
        if (excludeId.HasValue)
        {
            command.Parameters.AddWithValue("@Id", excludeId.Value);
        }
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // MAPEO - Convierte un SqlDataReader en objeto Mesa
    private Mesa Mapear(SqlDataReader reader)
    {
        return new Mesa
        {
            IdMesa = reader.GetInt32(0),
            Numero = reader.GetInt32(1),
            Estado = reader.GetString(2),
            Activo = reader.GetBoolean(3)
        };
    }
}
