using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using System;
using System.IO;

namespace Mobius.ILasm.Core.BenchmarkDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            //using Microsoft.Extensions.Hosting.IHost host = CreateHostBuilder(args).Build();
            RunBenchmark();
        }

        private static void RunBenchmark()
        {
            BenchmarkRunner.Run<BasicBenchmark>();
        }

        //private static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureServices((_, services) =>
        //            services.AddScoped<ILoggerFactory, LoggerFactory>());
    }
}
