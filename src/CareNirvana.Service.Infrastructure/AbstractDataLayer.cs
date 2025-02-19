using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CareNirvana.DataAccess
{
    public class AbstractDataLayer : IAbstractDataLayer
    {
        private readonly string _connectionString;

        public AbstractDataLayer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public string ConnectionString => _connectionString;

        public IDataReader ExecuteDataReader(string query)
        {
            var connection = new NpgsqlConnection(_connectionString);
            var command = new NpgsqlCommand(query, connection);
            connection.Open();
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteDataReader(string query, Dictionary<string, object> parameterWithValues)
        {
            var connection = new NpgsqlConnection(_connectionString);
            var command = new NpgsqlCommand(query, connection);
            AddParameters(command, parameterWithValues);
            connection.Open();
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public object ExecuteScalar(string query)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(query, connection);
            connection.Open();
            return command.ExecuteScalar();
        }

        public object ExecuteScalar(string query, Dictionary<string, object> parameterWithValues)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(query, connection);
            AddParameters(command, parameterWithValues);
            connection.Open();
            return command.ExecuteScalar();
        }

        public int ExectuteNonQuery(string query)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(query, connection);
            connection.Open();
            return command.ExecuteNonQuery();
        }

        public int ExectuteNonQuery(string query, Dictionary<string, object> parameterWithValues)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(query, connection);
            AddParameters(command, parameterWithValues);
            connection.Open();
            return command.ExecuteNonQuery();
        }

        private static void AddParameters(NpgsqlCommand command, Dictionary<string, object> parameterWithValues)
        {
            foreach (var param in parameterWithValues)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }
    }
}
