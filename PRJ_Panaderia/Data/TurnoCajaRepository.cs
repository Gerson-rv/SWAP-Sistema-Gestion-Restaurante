using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Turnos de Caja - Acceso a datos de tabla TurnosCaja
public class TurnoCajaRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public TurnoCajaRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Lista turnos de caja con filtros (abiertos/cerrados, fechas)
    public List<TurnoCaja> Listar(bool? soloAbiertos, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var turnos = new List<TurnoCaja>();
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT t.IdTurno, t.IdEmpleado, t.FechaApertura, t.FechaCierre,
                           t.MontoInicial, t.MontoCierre, t.Observaciones,
                           e.NombreCompleto AS NombreEmpleado
                    FROM TurnosCaja t
                    INNER JOIN Empleado e ON t.IdEmpleado = e.IdEmpleado";

        var conditions = new List<string>();

        if (soloAbiertos == true)
            conditions.Add("t.FechaCierre IS NULL");
        else if (soloAbiertos == false)
            conditions.Add("t.FechaCierre IS NOT NULL");

        if (fechaInicio.HasValue)
            conditions.Add("t.FechaApertura >= @FechaInicio");

        if (fechaFin.HasValue)
            conditions.Add("t.FechaApertura <= @FechaFin");

        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);

        sql += " ORDER BY t.FechaApertura DESC";

        using var command = new SqlCommand(sql, connection);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value.AddDays(1).AddSeconds(-1));

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            turnos.Add(Mapear(reader));
        }
        return turnos;
    }

    // SELECT - Obtiene un turno de caja por su ID
    public TurnoCaja? ObtenerPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT t.IdTurno, t.IdEmpleado, t.FechaApertura, t.FechaCierre,
                     t.MontoInicial, t.MontoCierre, t.Observaciones,
                     e.NombreCompleto AS NombreEmpleado
              FROM TurnosCaja t
              INNER JOIN Empleado e ON t.IdEmpleado = e.IdEmpleado
              WHERE t.IdTurno = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
            return Mapear(reader);
        return null;
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
            return Mapear(reader);
        return null;
    }

    // SELECT - Verifica si un empleado tiene turno abierto
    public bool ExisteTurnoAbiertoPorEmpleado(int idEmpleado, int? excludeId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT COUNT(1) FROM TurnosCaja WHERE IdEmpleado = @IdEmpleado AND FechaCierre IS NULL";
        if (excludeId.HasValue)
            sql += " AND IdTurno != @Id";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdEmpleado", idEmpleado);
        if (excludeId.HasValue)
            command.Parameters.AddWithValue("@Id", excludeId.Value);
        connection.Open();
        return (int)command.ExecuteScalar() > 0;
    }

    // INSERT - Crea un nuevo turno de caja y retorna el ID generado
    public int Crear(TurnoCaja turno, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO TurnosCaja (IdEmpleado, MontoInicial, Observaciones)
              VALUES (@IdEmpleado, @MontoInicial, @Observaciones);
              SELECT SCOPE_IDENTITY();", connection);
        command.Parameters.AddWithValue("@IdEmpleado", turno.IdEmpleado);
        command.Parameters.AddWithValue("@MontoInicial", turno.MontoInicial);
        command.Parameters.AddWithValue("@Observaciones", (object?)turno.Observaciones ?? DBNull.Value);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        _auditRepo.Registrar("TurnosCaja", id, "INSERT", empleadoId,
            $"Turno aperturado con S/{turno.MontoInicial:N2}");
        return id;
    }

    // UPDATE - Actualiza los datos de un turno de caja existente
    public void Actualizar(TurnoCaja turno, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE TurnosCaja
              SET IdEmpleado = @IdEmpleado, MontoInicial = @MontoInicial, Observaciones = @Observaciones
              WHERE IdTurno = @Id", connection);
        command.Parameters.AddWithValue("@Id", turno.IdTurno);
        command.Parameters.AddWithValue("@IdEmpleado", turno.IdEmpleado);
        command.Parameters.AddWithValue("@MontoInicial", turno.MontoInicial);
        command.Parameters.AddWithValue("@Observaciones", (object?)turno.Observaciones ?? DBNull.Value);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("TurnosCaja", turno.IdTurno, "UPDATE", empleadoId,
            $"Turno ID {turno.IdTurno} actualizado");
    }

    // UPDATE - Cierra un turno de caja con monto de cierre
    public void CerrarTurno(int id, decimal montoCierre, string? observaciones, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE TurnosCaja
              SET FechaCierre = SYSDATETIME(), MontoCierre = @MontoCierre, Observaciones = @Observaciones
              WHERE IdTurno = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@MontoCierre", montoCierre);
        command.Parameters.AddWithValue("@Observaciones", (object?)observaciones ?? DBNull.Value);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("TurnosCaja", id, "UPDATE", empleadoId,
            $"Turno ID {id} cerrado con S/{montoCierre:N2}");
    }

    // SELECT - Cuenta pedidos asociados a un turno
    public int ContarPedidos(int idTurno)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT COUNT(1) FROM Pedidos WHERE TurnoID = @IdTurno", connection);
        command.Parameters.AddWithValue("@IdTurno", idTurno);
        connection.Open();
        return (int)command.ExecuteScalar();
    }

    // DELETE - Elimina un turno de caja por su ID
    public void Eliminar(int id, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("DELETE FROM TurnosCaja WHERE IdTurno = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("TurnosCaja", id, "DELETE", empleadoId,
            $"Turno ID {id} eliminado");
    }

    // SELECT - Lista empleados activos para dropdowns
    public List<Empleado> ObtenerEmpleadosActivos()
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

    // MAPEO - Convierte un SqlDataReader en objeto TurnoCaja
    private TurnoCaja Mapear(SqlDataReader reader)
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
}