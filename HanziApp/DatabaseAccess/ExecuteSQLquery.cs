using System.Diagnostics;
using Azure.Identity;
using Dapper;
using Interfaces.DatabaseAccess;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DatabaseAccess
{
    public class ExecuteSQLquery : IExecuteSQLquery
    {
        private readonly IConfiguration _configuration; 
        public ExecuteSQLquery(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // RETURN INT
        // only for when changes are made to rows, will not work with SELECT queries
        public async Task<int> ExecuteQueryAsync_ReturnInt<GenericParameters>(GenericParameters parameters, string sqlQuery)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                int affectedRows = await connection.ExecuteAsync(sqlQuery, parameters);

                return affectedRows;
            }            
        }

        // RETURN MODEL
        public async Task<Model> ExecuteQueryAsync_ReturnDTO<Model, GenericParameters>(GenericParameters parameters, string sqlQuery)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                Model model = await connection.QueryFirstOrDefaultAsync<Model>(sqlQuery, parameters);

                return model;
            }           
        }

        // RETURN MODEL LIST
        public async Task<List<ModelList>> ExecuteQueryAsync_ReturnDTOlist<ModelList, GenericParameters>(GenericParameters parameters, string sqlQuery)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                List<ModelList> list = (await connection.QueryAsync<ModelList>(sqlQuery, parameters)).ToList(); 

                return list;
            }            
        }
    }
}
