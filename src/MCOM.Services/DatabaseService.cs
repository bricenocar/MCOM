using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MCOM.Services
{
    public interface IDatabaseService
    {
        Task<DataTable> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object> inputParameters);
    }

    public class DatabaseService : IDatabaseService
    {      
        private readonly IConfiguration _configuration;
        
        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string getConnectionString()
        {
            return _configuration.GetConnectionString("MCOMGovernanceDatabaseConnection");
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object> inputParameters)
        {
            var connectionString = getConnectionString();
            DataTable result = null;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // execute stored procedure with parameters using sql command
                using (var command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var input in inputParameters)
                    {
                        command.Parameters.AddWithValue(input.Key, input.Value);
                    }

                    using (SqlDataReader dataReader = await command.ExecuteReaderAsync())
                    {
                        result.Load(dataReader);
                    }
                }
            }   

            return result;
        }        
    }
}
