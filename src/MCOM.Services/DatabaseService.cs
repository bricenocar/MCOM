using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MCOM.Services
{
    public interface IDataBaseService
    {
        Task<DataTable> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object> inputParameters = null);
    }

    public class DataBaseService : IDataBaseService
    {
        private readonly IConfiguration _configuration;

        public DataBaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetValue<string>("MCOMGovernanceDatabaseConnection");
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object> inputParameters = null)
        {
            var connectionString = GetConnectionString();
            var result = new DataTable();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // execute stored procedure with parameters using sql command
                using var command = new SqlCommand(storedProcedureName, connection);
                command.CommandType = CommandType.StoredProcedure;

                if (inputParameters != null)
                {
                    foreach (var input in inputParameters)
                    {
                        command.Parameters.AddWithValue(input.Key, input.Value);
                    }
                }

                using SqlDataReader dataReader = await command.ExecuteReaderAsync();
                result.Load(dataReader);
            }

            return result;
        }
    }
}
