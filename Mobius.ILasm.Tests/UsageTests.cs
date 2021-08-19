using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Mobius.ILasm.Core;
using Xunit;

namespace Mobius.ILasm.Tests
{
    public class UsageTests
    {
        [Fact]
        public void AssembleLoadIntoAssemblyLoadContextAndExecute()
        {
            var logger = new Logger();
            var driver = new Driver(logger, Driver.Target.Dll, false, false, false);
            var cil = File.ReadAllText(@"./trivial/helloworldconsole.il");
            using var memoryStream = new MemoryStream();

            driver.Assemble(new [] { cil }, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var assemblyContext = new AssemblyLoadContext(null);
            var assembly = assemblyContext.LoadFromStream(memoryStream);
            var entryPoint = assembly.EntryPoint;
            var result = entryPoint?.Invoke(null, new object[] { new string[] { } });

            Assert.Equal(44, result);
        }
    }
}
