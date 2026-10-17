using Common.Logging;
using Discount.API.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Discount.API
{
    /// <summary>
    /// Defines the Program type.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Describes the Main operation.
        /// </summary>
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.MigrateDatabase<Program>();
            host.Run();
        }

        /// <summary>
        /// Executes the CreateHostBuilder operation.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog(SeriLogger.Configure)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });       
    }
}
