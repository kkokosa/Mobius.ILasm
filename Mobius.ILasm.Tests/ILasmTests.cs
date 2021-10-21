using Mobius.ILasm.Core;
using Mobius.ILasm.interfaces;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Mobius.ILasm.Tests.SourceGenerator;
using Mobius.ILasm.infrastructure;
using Moq;
using System;

namespace Mobius.ILasm.Tests
{
    // TODO: Create source generator to iterate through directory and generate test methods
    // [GenerateMethods("./trivial/*.il")]
    [GenerateTestMethods("./trivial/*.il")]
    public partial class ILasmTests
    {
        [Theory]
        [InlineData("helloworldconsole.il")]
        [InlineData("resource.il")]
        public void Test(string fileName) 
            => AssembleAndVerify($"./trivial/{fileName}");

        private static void AssembleAndVerify(string filename)
        {
            var logger = new Mock<ILogger>();
            var driver = new Driver(logger.Object, Driver.Target.Exe, new DriverSettings
            {
                ResourceResolver = new FileResourceResolver("./trivial")
            });
            using var memoryStream = new MemoryStream();
            var success = driver.Assemble(new [] { File.ReadAllText(filename) }, memoryStream);
            Assert.True(success, string.Join(
                Environment.NewLine, logger.Invocations
                    .Where(i => i.Method.Name == nameof(ILogger.Error))
                    .Select(i => i.Arguments.Last())
            ));

            var buffer = memoryStream.ToArray();
            var assembly = Assembly.Load(buffer);
            var entryPoint = assembly.EntryPoint;
            var result = entryPoint.Invoke(null, new object[] { new string[] { } });

            var assertLine = File.ReadLines(filename)
                                 .FirstOrDefault(line => line.StartsWith("// Assert result"));
            Assert.NotNull(assertLine);
            
            var match = Regex.Match(assertLine, @"// Assert result (\d+)");
            Assert.True(match.Success);

            var expected = int.Parse(match.Groups[1].Value);
            Assert.Equal(expected, result);
        }
    }
}
