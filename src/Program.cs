using System;
using System.Threading.Tasks;
using AzureBillingExporter.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Elasticsearch;

namespace AzureBillingExporter
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // The initial "bootstrap" logger is able to log errors during start-up. It's completely replaced by the
            // logger configured in `UseSerilog()` below, once configuration and dependency-injection have both been
            // set up successfully.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            Log.Information("Starting up");

            try
            {
                await CreateHostBuilder(args).Build().RunAsync();

                Log.Information("Stopped cleanly");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder  = Host.CreateDefaultBuilder(args);

            builder.UseSerilog(ConfigureLogger)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

            return builder;
        }

        private static void ConfigureLogger(HostBuilderContext context, IServiceProvider services,
            LoggerConfiguration configuration)
        {
            var config = context.Configuration.Get<EnvironmentConfiguration>();

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();

            if (config.LogsAtJsonFormat)
            {
                configuration.WriteTo.Console(new ElasticsearchJsonFormatter(inlineFields: true));
            }
            else
            {
                configuration.WriteTo.Console();
            }
        }
    }
}
