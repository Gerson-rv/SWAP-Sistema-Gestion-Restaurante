using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Empleados - Acceso a datos de tabla Empleado
public class EmpleadoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public EmpleadoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todos los empleados con INNER JOIN a Cargo
    public List<Empleado> Listar()
    {
        var empleados = new List<Empleado>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT e.IdEmpleado, e.IdCargo, e.NombreCompleto, e.Dni, e.Usuario,
                     e.Contrasena, e.Telefono, e.Activo, e.FechaCreacion,
                     c.Nombre AS NombreCargo
              FROM Empleado e
              INNER JOIN Cargo c ON e.IdCargo = c.IdCargo
              ORDER BY e.IdEmpleado DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            empleados.Add(Mapear(reader));
        }
        return empleados;
    }

    // SELECT - Obtiene un empleado por ID con JOIN a Cargo
    public Empleado? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT e.IdEmpleado, e.IdCargo, e.NombreCompleto, e.Dni, e.Usuario,
                     e.Contrasena, e.Telefono, e.Activo, e.FechaCreacion,
                     c.Nombre AS NombreCargo
              FROM Empleado e
              INNER JOIN Cargo c ON e.IdCargo = c.IdCargo
              WHERE e.IdEmpleado = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // INSERT - Crea un nuevo empleado y retorna el ID generado
    public int Crear(Empleado empleado, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO Empleado (IdCargo, NombreCompleto, Dni, Usuario, Contrasena, Telefono, Activo)
              VALUES (@IdCargo, @NombreCompleto, @Dni, @Usuario, @Contrasena, @Telefono, @Activo);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@IdCargo", empleado.IdCargo);
        command.Parameters.AddWithValue("@NombreCompleto", empleado.NombreCompleto);
        command.Parameters.AddWithValue("@Dni", empleado.Dni);
        command.Parameters.AddWithValue("@Usuario", empleado.Usuario);
        command.Parameters.AddWithValue("@Contrasena", empleado.Contrasena);
        command.Parameters.AddWithValue("@Telefono", (object?)empleado.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", empleado.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Empleado", id, "INSERT", empleadoId,
            $"Empleado '{empleado.NombreCompleto}' (Usuario: {empleado.Usuario}) creado");
        return id;
    }

    // UPDATE - Actualiza los datos de un empleado existente
    public void Actualizar(Empleado empleado, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Empleado SET IdCargo = @IdCargo, NombreCompleto = @NombreCompleto, Dni = @Dni,
                     Usuario = @Usuario, Contrasena = @Contrasena, Telefono = @Telefono,
                     Activo = @Activo
              WHERE IdEmpleado = @Id", connection);
        command.Parameters.AddWithValue("@Id", empleado.IdEmpleado);
        command.Parameters.AddWithValue("@IdCargo", empleado.IdCargo);
        command.Parameters.AddWithValue("@NombreCompleto", empleado.NombreCompleto);
        command.Parameters.AddWithValue("@Dni", empleado.Dni);
        command.Parameters.AddWithValue("@Usuario", empleado.Usuario);
        command.Parameters.AddWithValue("@Contrasena", empleado.Contrasena);
        command.Parameters.AddWithValue("@Telefono", (object?)empleado.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activo", empleado.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Empleado", empleado.IdEmpleado, "UPDATE", empleadoId,
            $"Empleado '{empleado.NombreCompleto}' actualizado - Estado: {(empleado.Activo ? "Activo" : "Inactivo")}");
    }

    // DELETE - Elimina un empleado por su ID (excepto si es el admin actual)
    public bool Eliminar(int id, int empleadoId = 1)
    {
        // Validar que no se esté eliminando al mismo admin que ejecuta la acción
        if (id == empleadoId)
            return false;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Verificar si el empleado tiene pedidos asociados
        using (var validar = new SqlCommand(
            "SELECT COUNT(*) FROM Pedidos WHERE EmpleadoID = @Id",
            connection))
        {
            validar.Parameters.AddWithValue("@Id", id);
            int cantidad = Convert.ToInt32(validar.ExecuteScalar());

            if (cantidad > 0)
                return false;
        }

        // Verificar si es el único administrador (opcional)
        using (var verificarAdmin = new SqlCommand(
            @"SELECT COUNT(*) FROM Empleado e
          INNER JOIN Cargo c ON e.IdCargo = c.IdCargo
          WHERE c.Nombre = 'Admin' AND e.IdEmpleado != @Id AND e.Activo = 1",
            connection))
        {
            verificarAdmin.Parameters.AddWithValue("@Id", id);
            int otrosAdmins = Convert.ToInt32(verificarAdmin.ExecuteScalar());

            // Si el empleado es admin y es el único admin activo, no permitir eliminar
            using (var esAdmin = new SqlCommand(
                @"SELECT COUNT(*) FROM Empleado e
              INNER JOIN Cargo c ON e.IdCargo = c.IdCargo
              WHERE c.Nombre = 'Admin' AND e.IdEmpleado = @Id",
                connection))
            {
                esAdmin.Parameters.AddWithValue("@Id", id);
                int esAdminCount = Convert.ToInt32(esAdmin.ExecuteScalar());

                if (esAdminCount > 0 && otrosAdmins == 0)
                    return false;
            }
        }

        using var command = new SqlCommand(
            "DELETE FROM Empleado WHERE IdEmpleado = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", id);

        int filas = command.ExecuteNonQuery();

        if (filas > 0)
        {
            _auditRepo.Registrar(
                "Empleado",
                id,
                "DELETE",
                empleadoId,
                $"Empleado ID {id} eliminado");

            return true;
        }

        return false;
    }

    // SELECT - Lista los cargos activos para dropdowns
    public List<Cargo> ObtenerCargosActivos()
    {
        var cargos = new List<Cargo>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCargo, Nombre FROM Cargo WHERE Activo = 1 ORDER BY Nombre", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cargos.Add(new Cargo
            {
                IdCargo = reader.GetInt32(0),
                Nombre = reader.GetString(1)
            });
        }
        return cargos;
    }

    // SELECT - Obtiene el nombre del cargo por su ID
    public string? ObtenerNombreCargo(int idCargo)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT Nombre FROM Cargo WHERE IdCargo = @Id", connection);
        command.Parameters.AddWithValue("@Id", idCargo);
        connection.Open();
        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    // SELECT - Verifica si ya existe un empleado con el mismo DNI
    public bool ExisteDni(string dni, int? excludeId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM Empleado WHERE Dni = @Dni";
        if (excludeId.HasValue) sql += " AND IdEmpleado != @Id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Dni", dni);
        if (excludeId.HasValue) command.Parameters.AddWithValue("@Id", excludeId.Value);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // SELECT - Verifica si ya existe un empleado con el mismo usuario
    public bool ExisteUsuario(string usuario, int? excludeId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM Empleado WHERE Usuario = @Usuario";
        if (excludeId.HasValue) sql += " AND IdEmpleado != @Id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Usuario", usuario);
        if (excludeId.HasValue) command.Parameters.AddWithValue("@Id", excludeId.Value);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // SELECT - Verifica si ya existe un empleado con el mismo telefono
    public bool ExisteTelefono(string telefono, int? excludeId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM Empleado WHERE Telefono = @Telefono";
        if (excludeId.HasValue) sql += " AND IdEmpleado != @Id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Telefono", telefono);
        if (excludeId.HasValue) command.Parameters.AddWithValue("@Id", excludeId.Value);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // MAPEO - Convierte un SqlDataReader en objeto Empleado
    private Empleado Mapear(SqlDataReader reader)
    {
        return new Empleado
        {
            IdEmpleado = reader.GetInt32(0),
            IdCargo = reader.GetInt32(1),
            NombreCompleto = reader.GetString(2),
            Dni = reader.GetString(3),
            Usuario = reader.GetString(4),
            Contrasena = reader.GetString(5),
            Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
            Activo = reader.GetBoolean(7),
            FechaCreacion = reader.GetDateTime(8),
            NombreCargo = reader.GetString(9)
        };
    }
}
