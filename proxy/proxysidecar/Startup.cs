using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using ProxyKit;

namespace emptysidecar
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
            services.AddSingleton<Repository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IConfiguration config, Repository repo, ILogger<Startup> logger)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var timeout = config.GetValue<int>("CacheTimeSeconds");

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(timeout));

            var alias = config.GetValue<string>("Downstream:Alias");
            var port = config.GetValue<string>("Downstream:Port");
            var permission = config.GetValue<string>("Permission");

            app.RunProxy(context => {
                var user = AuthHelper.GetAuthUser(context.Request.Headers["Authorization"]);

                logger.LogInformation("User:" + user);
                logger.LogInformation("Permission:" + permission);

                if (!string.IsNullOrEmpty(user))
                {              
                    var forwardContext = context.ForwardTo(String.Format("http://{0}:{1}",alias, port));

                    if (!cache.TryGetValue(user, out bool hasPermission))
                    {
                        hasPermission = repo.UserHasPermission(permission, user);
                        cache.Set(user, hasPermission, cacheEntryOptions);
                    }

                    if (hasPermission)
                    {
                        forwardContext.UpstreamRequest.Headers.Add("Permission", permission);
                            
                    }
                    return forwardContext.Send();
                }
                else
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }
            }
            );
        }
    }
}
