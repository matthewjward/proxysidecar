using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public void Configure(IApplicationBuilder app, IConfiguration config, Repository repo)
        {
            app.RunProxy(context => {
                var user = AuthHelper.GetAuthUser(context.Request.Headers["Authorization"]);

                if (!string.IsNullOrEmpty(user))
                {
                    var alias = config.GetValue<string>("Downstream:Alias");
                    var port = config.GetValue<string>("Downstream:Port");

                    var forwardContext = context.ForwardTo(String.Format("http://{0}:{1}",alias, port));
                    if (repo.HasSpecialPower(user))
                    {
                        forwardContext.UpstreamRequest.Headers.Add("HasSpecialPower", "");
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
