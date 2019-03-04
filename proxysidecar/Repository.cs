using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace emptysidecar
{
    public class Repository
    {
        string _connString;
        int _timeout; 
        MemoryCache _cache;

        public Repository(IConfiguration config)
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _timeout = config.GetValue<int>("Sidecar:CacheTimeSeconds");

            var host = config.GetValue<string>("Sidecar:Database:Host");
            var username = config.GetValue<string>("Sidecar:Database:Username");
            var password = config.GetValue<string>("Sidecar:Database:Password");
            var database = config.GetValue<string>("Sidecar:Database:Database");

            _connString = String.Format("Host={0};Username={1};Password={2};Database={3}", host, username, password, database);
        }

        public bool HasSpecialPower(string name)
        {
            bool hasSpecialPower;

            // Look for cache key.
            if (!_cache.TryGetValue(name, out hasSpecialPower))
            {
                // Key not in cache, so get data.
                hasSpecialPower = HitTheDatabase(name);

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_timeout));

                // Save data in cache.
                _cache.Set(name, hasSpecialPower, cacheEntryOptions);
            }

            return hasSpecialPower;
        }

        private bool HitTheDatabase(string name)
        {
            bool hasSpecialPower = false;
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                // Insert some data
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT HasSpecialPower FROM users WHERE Name = @name";
                    cmd.Parameters.AddWithValue("name", name);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        hasSpecialPower = (bool)result;
                    }
                }
            }
            return hasSpecialPower;
        }
    }
}
