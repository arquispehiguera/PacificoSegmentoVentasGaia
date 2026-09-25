using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace PacificoSegmentoVentasGaia.Infraestructure.Data;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AppConnection")
            ?? throw new InvalidOperationException("Connection string 'AppConnection' not found.");
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
