using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace AzureBillingExporter
{
    public class Startup
    {
        private static readonly AzureBillingMetricsGrapper BillingGrapper = new AzureBillingMetricsGrapper();
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            
            Metrics.DefaultRegistry.AddBeforeCollectCallback(async (cancel) =>
                {
                    await BillingGrapper.DownloadFromApi(cancel);
                });
            
            // ASP.NET Core 3 or newer
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
            });
        }
    }
}