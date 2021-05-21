using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobius.ILasm.Core.BenchmarkDotNet
{
    [MemoryDiagnoser]
    [NativeMemoryProfiler]
    public class BasicBenchmark
    {
        private ILogger _logger;

        [GlobalSetup]
        public void Setup()
        {
            _logger = new NullLogger();
        }

        [Benchmark]
        public void GenerateDynamicallyLinkedLibrary()
        {
            //// TODO: make it work :)
            //var driver = new Driver(_logger);
            //using (var memoryStream = new MemoryStream(...))
            //{
            //    driver.Assemble(new string[] { "resources/helloworldconsole.il", "/dll" }, memoryStream);
            //}
        }

        [Benchmark]
        public void GenerateExecutable()
        {
            var driver = new Driver(_logger);
            //using (var memoryStream = new MemoryStream(...))
            //{
            //    driver.Assemble(new string[] { "resources/helloworldconsole.il", "/exe" });
            //}
        }
    }
}
