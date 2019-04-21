using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace emptysidecar
{
    public class Repository
    {
        private readonly string _connString;
        private readonly ILogger _logger;

        public Repository(IConfiguration config, ILogger<Repository> logger)
        {
            _logger = logger;

            var host = config.GetValue<string>("Database:Host");
            var username = config.GetValue<string>("Database:Username");
            var password = config.GetValue<string>("Database:Password");
            var database = config.GetValue<string>("Database:Database");

            _connString = String.Format("Host={0};Username={1};Password={2};Database={3}", host, username, password, database);
        }

        public bool UserHasPermission(string permission, string name)
        {
            bool permissionValue = false;
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    // Insert some data
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT "+permission+" FROM users WHERE Name = @name";
                        cmd.Parameters.AddWithValue("permission", permission);
                        cmd.Parameters.AddWithValue("name", name);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            _logger.LogInformation("DBResult:" + result);
                            permissionValue = (bool)result;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                _logger.LogError("Database Error" + e.Message);
            }

            return permissionValue;
        }
    }
}
