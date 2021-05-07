using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using System;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            using Microsoft.Extensions.Hosting.IHost host = CreateHostBuilder(args).Build();
            RunILasm(host.Services, args);
          
        }

        private static void RunILasm(IServiceProvider services, string[] args)
        {
            using IServiceScope serviceScope = services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            var driver = new Driver(loggerFactory);
            driver.Assemble(args);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services.AddScoped<interfaces.ILoggerFactory, LoggerFactory>());
    }
}
