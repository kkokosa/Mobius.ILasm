using System.IO;
using System.Runtime.Loader;
using Mobius.ILasm.Core;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using Moq;
using Xunit;

namespace Mobius.ILasm.Tests
{
    public class UsageTests
    {
        [Fact]
        public void AssembleLoadIntoAssemblyLoadContextAndExecute()
        {
            var driver = new Driver(Mock.Of<ILogger>(), Driver.Target.Dll);
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
