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
        private ILoggerFactory loggerFactory;
        public BasicBenchmark()
        {
            this.loggerFactory = new LoggerFactory();
        }
        [Benchmark]
        public void GenerateDynamicallyLinkedLibrary()
        {
            var driver = new Driver(loggerFactory);
            driver.Assemble(new string[] {"resources/helloworldconsole.il", "/dll" });
        }

        [Benchmark]
        public void GenerateExecutable()
        {
            var driver = new Driver(loggerFactory);
            driver.Assemble(new string[] { "resources/helloworldconsole.il", "/exe" });
        }
    }
}
