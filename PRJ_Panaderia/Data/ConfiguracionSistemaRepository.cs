using Microsoft.Data.SqlClient;
using PRJ_Panaderia.Models;

namespace PRJ_Panaderia.Data;

// Repositorio de Configuración del Sistema - Acceso a datos de tabla ConfiguracionSistema
public class ConfiguracionSistemaRepository
{
    private readonly string _connectionString;
    private readonly AuditoriaRepository _auditRepo;

    public ConfiguracionSistemaRepository(IConfiguration configuration, AuditoriaRepository auditRepo)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
        _auditRepo = auditRepo;
    }

    // SELECT - Obtiene la configuración del sistema
    public ConfiguracionSistema? Obtener()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT ConfigID, NombreNegocio, RUC, RazonSocial, IGV_Porcentaje, Moneda, NumeroYape, Correo
              FROM ConfiguracionSistema WHERE ConfigID = 1", connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Mapear(reader);
        }
        return null;
    }

    // UPDATE - Actualiza la configuración del sistema
    public void Actualizar(ConfiguracionSistema config, int empleadoId = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE ConfiguracionSistema 
              SET NombreNegocio = @NombreNegocio,
                  RUC = @RUC,
                  RazonSocial = @RazonSocial,
                  IGV_Porcentaje = @IGV_Porcentaje,
                  Moneda = @Moneda,
                  NumeroYape = @NumeroYape,
                  Correo = @Correo
              WHERE ConfigID = 1", connection);
        command.Parameters.AddWithValue("@NombreNegocio", config.NombreNegocio);
        command.Parameters.AddWithValue("@RUC", config.RUC);
        command.Parameters.AddWithValue("@RazonSocial", config.RazonSocial);
        command.Parameters.AddWithValue("@IGV_Porcentaje", config.IGV_Porcentaje);
        command.Parameters.AddWithValue("@Moneda", config.Moneda);
        command.Parameters.AddWithValue("@NumeroYape", (object?)config.NumeroYape ?? DBNull.Value);
        command.Parameters.AddWithValue("@Correo", config.Correo);
        connection.Open();
        command.ExecuteNonQuery();
        _auditRepo.Registrar("ConfiguracionSistema", 1, "UPDATE", empleadoId,
            $"Configuracion actualizada: {config.NombreNegocio} (RUC: {config.RUC})");
    }

    // MAPEO - Convierte un SqlDataReader en objeto ConfiguracionSistema
    private ConfiguracionSistema Mapear(SqlDataReader reader)
    {
        return new ConfiguracionSistema
        {
            ConfigID = reader.GetInt32(0),
            NombreNegocio = reader.GetString(1),
            RUC = reader.GetString(2),
            RazonSocial = reader.GetString(3),
            IGV_Porcentaje = reader.GetDecimal(4),
            Moneda = reader.GetString(5),
            NumeroYape = reader.IsDBNull(6) ? null : reader.GetString(6),
            Correo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
        };
    }
}
