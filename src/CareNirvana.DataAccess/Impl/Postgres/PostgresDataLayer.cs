using System.Data;
using Npgsql;
using iCare4H.Common;

namespace iCare4H.DataAccess.Impl.Postgres
{
    public class PostgresDataLayer : IAbstractDataLayer
    {
        private readonly string connectionString;
        private readonly int timeOut;
        private readonly int retryCount;

        public PostgresDataLayer(
                string serverName, 
                string databaseName, 
                string userName, 
                string password, 
                int port, 
                int timeOut = 180, 
                int retryCount = 3)
        {
            connectionString = BuildConnectionString(serverName, databaseName, userName, password, port);
            this.timeOut = timeOut;
            this.retryCount = retryCount;
        }

        public PostgresDataLayer(
                string connectionString,
                int timeOut = 180,
                int retryCount = 3)
        {
            this.connectionString = connectionString;
            this.timeOut = timeOut;
            this.retryCount = retryCount;
        }

        public string ConnectionString => connectionString;

        public int ExectuteNonQuery(string query)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, int>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, []).ExecuteNonQuery());
        }

        public int ExectuteNonQuery(string query, Dictionary<string, object> parmeterWithValues)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, int>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, parmeterWithValues).ExecuteNonQuery());
        }

        public IDataReader ExecuteDataReader(string query)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, IDataReader>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, []).ExecuteReader());
        }

        public IDataReader ExecuteDataReader(string query, Dictionary<string, object> parmeterWithValues)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, IDataReader>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, parmeterWithValues).ExecuteReader());
        }

        public object ExecuteScalar(string query)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, object>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, []).ExecuteScalar());
        }

        public object ExecuteScalar(string query, Dictionary<string, object> parmeterWithValues)
        {
            return new ComputationRetryer(retryCount, query).Run<Exception, object>(() =>
                        BuildCommandWithParameters(CommandType.Text, query, parmeterWithValues).ExecuteScalar());
        }

        private string BuildConnectionString(string serverName, string databaseName, string userName, string password, int port)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                ApplicationName = nameof(iCare4H),
                Host = serverName,
                Database = databaseName,
                Port = port,
                Username = userName,
                Password = password,
                Timeout = timeOut,
                SslMode = SslMode.Require
            };

            return connectionStringBuilder.ToString();
        }

        private NpgsqlConnection Connection()
        {
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return connection;  
        }

        private IDbCommand BuildCommandWithParameters(
                CommandType commandType,
                string query,
                Dictionary<string, object> parameterWithValues)
        {
            var command = new NpgsqlCommand
            {
                CommandType = commandType,
                CommandTimeout = timeOut,
                Connection = Connection()
            };

            foreach (var parameter in parameterWithValues)
            {
                NpgsqlParameter npgsqlParameter;
                if (IsCommaSeparated(parameter.Value))
                {
                    var values = Convert.ToString(parameter.Value).Split(',');
                    var intermediateParameters  = new string[values.Length];

                    for(var i = 0; i <values.Length; i++)
                    {
                        if (values[i] != null)
                        {
                            intermediateParameters[i] = parameter.Key + i.ToString();
                            npgsqlParameter = new NpgsqlParameter(
                                        intermediateParameters[i], 
                                        values[i]);
                            command.Parameters.Add(npgsqlParameter);
                        }
                    }
                    query = query.Replace(parameter.Key,
                                    string.Join(",", intermediateParameters));
                }
                else
                {
                    npgsqlParameter = new NpgsqlParameter(parameter.Key, parameter.Value);
                    command.Parameters.Add(npgsqlParameter);
                }
            }

            return command;
        }

        private static bool IsCommaSeparated(object parameterWithValues)
        {
            return parameterWithValues.GetType() == typeof(string)
                && Convert.ToString(parameterWithValues).Contains(',');
        }
    }
}
