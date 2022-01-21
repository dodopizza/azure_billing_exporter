using System;
using AzureBillingExporter.AzureApi;
using Dodo.HttpClientResiliencePolicies;
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
            services.AddJsonClient<AzureCostManagementClient, AzureCostManagementClient>(
                new Uri("https://management.azure.com"), "AzureBillingExporter");
            services.Configure<ApiSettings>(Configuration.GetSection("ApiSettings"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApiSettings>>().Value);
            services.AddSingleton<BillingQueryClient>();
            services.AddSingleton<AzureCostManagementClient>();
            services.AddSingleton<AzureBillingMetricsGrapper>();
            services.AddSingleton<CostDataCache>();

            services.AddSingleton<CustomCollectorConfiguration>(serviceProvider=>
            {
                var customCollectorsFilePath = Configuration["CustomCollectorsFilePath"];
                return new CustomCollectorConfiguration(customCollectorsFilePath);
            });

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

            services.AddHostedService(resolver =>
            {
                var billingQueryClient = resolver.GetRequiredService<BillingQueryClient>();
                var costDataCache = resolver.GetRequiredService<CostDataCache>();
                var logger = resolver.GetRequiredService<ILogger<BackgroundCostCollectorHostedService>>();
                return new BackgroundCostCollectorHostedService(
                    billingQueryClient,
                    costDataCache,
                    logger);
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
