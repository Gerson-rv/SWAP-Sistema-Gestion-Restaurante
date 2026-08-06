using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Cargos - Acceso a datos de tabla Cargo
public class CargoRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public CargoRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista todos los cargos ordenados por ID descendente
    public List<Cargo> Listar()
    {
        var cargos = new List<Cargo>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCargo, Nombre, Sueldo, Activo FROM Cargo ORDER BY IdCargo DESC", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cargos.Add(new Cargo
            {
                IdCargo = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Sueldo = reader.GetDecimal(2),
                Activo = reader.GetBoolean(3)
            });
        }
        return cargos;
    }

    // SELECT - Obtiene un cargo por su ID
    public Cargo? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT IdCargo, Nombre, Sueldo, Activo FROM Cargo WHERE IdCargo = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Cargo
            {
                IdCargo = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Sueldo = reader.GetDecimal(2),
                Activo = reader.GetBoolean(3)
            };
        }
        return null;
    }

    // INSERT - Crea un nuevo cargo y retorna el ID generado
    public int Crear(Cargo cargo, int empleadoId = 1)
    {
        // Validaciones adicionales en el repositorio
        if (cargo.Sueldo <= 0)
            throw new ArgumentException("El sueldo debe ser mayor que cero.", nameof(cargo.Sueldo));

        if (cargo.Sueldo > 999999.99m)
            throw new ArgumentException("El sueldo no puede ser mayor a 999,999.99.", nameof(cargo.Sueldo));

        // Redondear a 2 decimales
        cargo.Sueldo = Math.Round(cargo.Sueldo, 2, MidpointRounding.AwayFromZero);

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "INSERT INTO Cargo (Nombre, Sueldo, Activo) VALUES (@Nombre, @Sueldo, @Activo); SELECT SCOPE_IDENTITY();",
            connection);
        command.Parameters.AddWithValue("@Nombre", cargo.Nombre);
        command.Parameters.AddWithValue("@Sueldo", cargo.Sueldo);
        command.Parameters.AddWithValue("@Activo", cargo.Activo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("Cargo", id, "INSERT", empleadoId,
            $"Cargo '{cargo.Nombre}' creado con sueldo {cargo.Sueldo:F2}");
        return id;
    }

    // UPDATE - Actualiza los datos de un cargo existente
    public void Actualizar(Cargo cargo, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "UPDATE Cargo SET Nombre = @Nombre, Sueldo = @Sueldo, Activo = @Activo WHERE IdCargo = @Id", connection);
        command.Parameters.AddWithValue("@Id", cargo.IdCargo);
        command.Parameters.AddWithValue("@Nombre", cargo.Nombre);
        command.Parameters.AddWithValue("@Sueldo", cargo.Sueldo);
        command.Parameters.AddWithValue("@Activo", cargo.Activo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Cargo", cargo.IdCargo, "UPDATE", empleadoId,
            $"Cargo '{cargo.Nombre}' actualizado - Estado: {(cargo.Activo ? "Activo" : "Inactivo")}");
    }

    // DELETE - Elimina un cargo por su ID
    public void Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);

        connection.Open();

        // 1. Validar si el cargo existe y obtener su nombre
        var checkCargoCmd = new SqlCommand(
            "SELECT Nombre FROM Cargo WHERE IdCargo = @Id", connection);

        checkCargoCmd.Parameters.AddWithValue("@Id", id);

        var nombreCargo = checkCargoCmd.ExecuteScalar()?.ToString();

        if (nombreCargo == null)
        {
            throw new Exception("El cargo no existe.");
        }

        // 2. Regla de negocio: no eliminar ADMIN
        if (nombreCargo == "Administrador")
        {
            throw new Exception("No se puede eliminar el cargo Administrador.");
        }

        // 3. Validar si tiene empleados asignados
        var checkEmpleadosCmd = new SqlCommand(
            "SELECT COUNT(1) FROM Empleado WHERE IdCargo = @IdCargo", connection);

        checkEmpleadosCmd.Parameters.AddWithValue("@IdCargo", id);

        int cantidadEmpleados = (int)checkEmpleadosCmd.ExecuteScalar();

        if (cantidadEmpleados > 0)
        {
            throw new Exception("No se puede eliminar el cargo porque tiene empleados asignados.");
        }

        // 4. Eliminar cargo
        var deleteCmd = new SqlCommand(
            "DELETE FROM Cargo WHERE IdCargo = @Id", connection);

        deleteCmd.Parameters.AddWithValue("@Id", id);
        deleteCmd.ExecuteNonQuery();

        // 5. Auditoría
        _auditRepo.Registrar(
            "Cargo",
            id,
            "DELETE",
            empleadoId,
            $"Cargo ID {id} eliminado correctamente"
        );
    }

    // SELECT - Cuenta empleados asignados a un cargo
    public int ContarEmpleados(int idCargo)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT COUNT(1) FROM Empleado WHERE IdCargo = @IdCargo", connection);
        command.Parameters.AddWithValue("@IdCargo", idCargo);
        connection.Open();
        return (int)command.ExecuteScalar();
    }

    // UPDATE - Actualiza solo el sueldo de un cargo
    public void ActualizarSueldo(int idCargo, decimal nuevoSueldo, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "UPDATE Cargo SET Sueldo = @Sueldo WHERE IdCargo = @Id", connection);
        command.Parameters.AddWithValue("@Id", idCargo);
        command.Parameters.AddWithValue("@Sueldo", Math.Round(nuevoSueldo, 2, MidpointRounding.AwayFromZero));
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("Cargo", idCargo, "UPDATE", empleadoId,
            $"Sueldo del cargo ID {idCargo} actualizado a {nuevoSueldo:F2}");
    }
}
