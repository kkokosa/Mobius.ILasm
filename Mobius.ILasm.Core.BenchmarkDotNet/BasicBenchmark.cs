using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
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
        [Benchmark]
        public void GenerateDynamicallyLinkedLibrary()
        {
            var driver = new Driver();
            driver.Assemble(new string[] {"resources/helloworldconsole.il", "/dll" });
        }

        [Benchmark]
        public void GenerateExecutable()
        {
            var driver = new Driver();
            driver.Assemble(new string[] { "resources/helloworldconsole.il", "/exe" });
        }
    }
}
