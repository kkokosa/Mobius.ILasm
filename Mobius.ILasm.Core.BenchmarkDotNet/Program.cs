using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mobius.ILasm.infrastructure;
using System;
using System.IO;

namespace Mobius.ILasm.Core.BenchmarkDotNet
{
    class Program
    {
        static void Main(string[] args)
        {            
            RunBenchmark();
        }

        private static void RunBenchmark()
        {
            BenchmarkRunner.Run<BasicBenchmark>();
        }        
    }
}
