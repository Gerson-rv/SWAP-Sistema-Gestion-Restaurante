using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Auditoría - Acceso a datos de tabla AuditoriaSistema
public class AuditoriaRepository
{
    private readonly string _connectionString;
    private readonly int _defaultEmpleadoId;

    public AuditoriaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexion 'DefaultConnection' no esta configurada.");
        _defaultEmpleadoId = configuration.GetValue<int>("AuditDefaultEmpleadoId");
    }

    // INSERT - Registra una acción de auditoría en el sistema
    public void Registrar(string tabla, int registroId, string accion, int? empleadoId, string? detalle)
    {
        var idUsado = empleadoId ?? _defaultEmpleadoId;
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"INSERT INTO AuditoriaSistema (Tabla, RegistroID, Accion, EmpleadoID, Fecha, Detalle)
              VALUES (@Tabla, @RegistroID, @Accion, @EmpleadoID, @Fecha, @Detalle)", connection);
        command.Parameters.AddWithValue("@Tabla", tabla);
        command.Parameters.AddWithValue("@RegistroID", registroId);
        command.Parameters.AddWithValue("@Accion", accion);
        command.Parameters.AddWithValue("@EmpleadoID", idUsado);
        command.Parameters.AddWithValue("@Fecha", DateTime.Now);
        command.Parameters.AddWithValue("@Detalle", (object?)detalle ?? DBNull.Value);
        connection.Open();
        command.ExecuteNonQuery();
    }

    // SELECT - Lista registros de auditoría con filtros y paginación
    public List<Auditoria> Listar(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? tabla = null, string? accion = null, int? empleadoId = null, string? busqueda = null, int pagina = 1, int tamPagina = 15)
    {
        var registros = new List<Auditoria>();
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT a.AuditoriaID, a.Tabla, a.RegistroID, a.Accion, a.EmpleadoID,
                           a.Fecha, a.Detalle,
                           e.NombreCompleto AS NombreEmpleado,
                           c.Nombre AS NombreCargo
                    FROM AuditoriaSistema a
                    LEFT JOIN Empleado e ON a.EmpleadoID = e.IdEmpleado
                    LEFT JOIN Cargo c ON e.IdCargo = c.IdCargo
                    WHERE 1=1";

        if (fechaInicio.HasValue)
            sql += " AND a.Fecha >= @FechaInicio";
        if (fechaFin.HasValue)
            sql += " AND a.Fecha < @FechaFin";
        if (!string.IsNullOrEmpty(tabla))
            sql += " AND a.Tabla = @Tabla";
        if (!string.IsNullOrEmpty(accion))
            sql += " AND a.Accion = @Accion";
        if (empleadoId.HasValue)
            sql += " AND a.EmpleadoID = @EmpleadoID";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (a.Detalle LIKE @Busqueda OR a.Tabla LIKE @Busqueda OR CAST(a.RegistroID AS VARCHAR) LIKE @Busqueda)";

        sql += " ORDER BY a.Fecha DESC, a.AuditoriaID DESC";
        sql += " OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

        using var command = new SqlCommand(sql, connection);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(tabla))
            command.Parameters.AddWithValue("@Tabla", tabla);
        if (!string.IsNullOrEmpty(accion))
            command.Parameters.AddWithValue("@Accion", accion);
        if (empleadoId.HasValue)
            command.Parameters.AddWithValue("@EmpleadoID", empleadoId.Value);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

        command.Parameters.AddWithValue("@Offset", (pagina - 1) * tamPagina);
        command.Parameters.AddWithValue("@Fetch", tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            registros.Add(new Auditoria
            {
                AuditoriaID = reader.GetInt32(0),
                Tabla = reader.GetString(1),
                RegistroID = reader.GetInt32(2),
                Accion = reader.GetString(3),
                EmpleadoID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Fecha = reader.GetDateTime(5),
                Detalle = reader.IsDBNull(6) ? null : reader.GetString(6),
                NombreEmpleado = reader.IsDBNull(7) ? "Sistema" : reader.GetString(7),
                NombreCargo = reader.IsDBNull(8) ? "-" : reader.GetString(8)
            });
        }
        return registros;
    }

    // SELECT - Cuenta registros de auditoría con filtros para paginación
    public int Contar(DateTime? fechaInicio = null, DateTime? fechaFin = null, string? tabla = null, string? accion = null, int? empleadoId = null, string? busqueda = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT COUNT(*)
                    FROM AuditoriaSistema a
                    WHERE 1=1";

        if (fechaInicio.HasValue)
            sql += " AND a.Fecha >= @FechaInicio";
        if (fechaFin.HasValue)
            sql += " AND a.Fecha < @FechaFin";
        if (!string.IsNullOrEmpty(tabla))
            sql += " AND a.Tabla = @Tabla";
        if (!string.IsNullOrEmpty(accion))
            sql += " AND a.Accion = @Accion";
        if (empleadoId.HasValue)
            sql += " AND a.EmpleadoID = @EmpleadoID";
        if (!string.IsNullOrEmpty(busqueda))
            sql += " AND (a.Detalle LIKE @Busqueda OR a.Tabla LIKE @Busqueda OR CAST(a.RegistroID AS VARCHAR) LIKE @Busqueda)";

        using var command = new SqlCommand(sql, connection);
        if (fechaInicio.HasValue)
            command.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            command.Parameters.AddWithValue("@FechaFin", fechaFin.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(tabla))
            command.Parameters.AddWithValue("@Tabla", tabla);
        if (!string.IsNullOrEmpty(accion))
            command.Parameters.AddWithValue("@Accion", accion);
        if (empleadoId.HasValue)
            command.Parameters.AddWithValue("@EmpleadoID", empleadoId.Value);
        if (!string.IsNullOrEmpty(busqueda))
            command.Parameters.AddWithValue("@Busqueda", "%" + busqueda + "%");

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // SELECT - Lista empleados activos para filtros de auditoría
    public List<Empleado> ObtenerEmpleados()
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

    // SELECT - Lista tablas con registros de auditoría
    public List<string> ObtenerTablas()
    {
        var tablas = new List<string>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT DISTINCT Tabla FROM AuditoriaSistema ORDER BY Tabla", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tablas.Add(reader.GetString(0));
        }
        return tablas;
    }
}
