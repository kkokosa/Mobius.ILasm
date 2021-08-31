using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using System;
using System.Collections.Generic;
using System.IO;
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
            var driver = new Driver(_logger, Driver.Target.Dll, false, false, false);
            using (var memoryStream = new MemoryStream())
            {
                driver.Assemble(new [] { File.ReadAllText("resources/helloworldconsole.il") }, memoryStream);
            }
        }

        [Benchmark]
        public void GenerateExecutable()
        {
            var driver = new Driver(_logger, Driver.Target.Exe, false, false, false);
            using (var memoryStream = new MemoryStream())
            {
                driver.Assemble(new [] { File.ReadAllText("resources/helloworldconsole.il") }, memoryStream);
            }
        }
    }
}
