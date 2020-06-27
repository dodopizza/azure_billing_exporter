using System;
using AzureBillingExporter.AzureApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace AzureBillingExporter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApiSettings>(Configuration.GetSection("ApiSettings"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApiSettings>>().Value);
            services.AddSingleton<BillingQueryClient>();
            services.AddSingleton<AzureCostManagementClient>();
            services.AddSingleton<AzureBillingMetricsGrapper>();
            
            
            services.AddSingleton<IAccessTokenFactory, AccessTokenFactory>();
            services.AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
            
            services.AddHostedService(resolver =>
            {
                var accessTokenProvider = resolver.GetRequiredService<IAccessTokenProvider>();
                var accessTokenFactory = resolver.GetRequiredService<IAccessTokenFactory>();
                var logger = resolver.GetRequiredService<ILogger<BackgroundAccessTokenProviderHostedService>>();
                return new BackgroundAccessTokenProviderHostedService(
                    accessTokenProvider,
                    accessTokenFactory,
                    TimeSpan.FromMinutes(50),
                    logger,
                    TimeSpan.FromSeconds(10));
            });
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            
            var billingGrapper = app.ApplicationServices.GetService<AzureBillingMetricsGrapper>();
            Metrics.DefaultRegistry.AddBeforeCollectCallback(async (cancel) =>
                {
                    await billingGrapper.DownloadFromApi(cancel);
                });
            
            // ASP.NET Core 3 or newer
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
            });
        }
    }
}