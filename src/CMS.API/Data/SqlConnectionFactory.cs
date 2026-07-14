using System.Data;
using Microsoft.Data.SqlClient;

namespace CMS.API.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CMS")
            ?? throw new InvalidOperationException("Connection string 'CMS' is not configured.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
