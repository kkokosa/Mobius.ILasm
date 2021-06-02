using Mobius.ILasm.Core;
using Mobius.ILasm.interfaces;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Mobius.ILasm.Tests
{
    // TODO: Create source generator to iterate through directory and generate test methods
    // [GenerateMethods("./trivial/*.il")]
    public class UnitTest1
    {
        [Fact]
        public void Test_helloworldconsole() 
            => AssembleAndVerify("./trivial/helloworldconsole.il");        

        private static void AssembleAndVerify(string filename)
        {
            var logger = new Logger();
            var driver = new Driver(logger);
            var memoryStream = new MemoryStream();
            var success = driver.Assemble(new string[] { filename, "/exe" }, memoryStream);

            var buffer = memoryStream.ToArray();
            var assembly = Assembly.Load(buffer);
            var entryPoint = assembly.EntryPoint;
            var result = entryPoint.Invoke(null, new object[] { new string[] { } });

            var assertLine = File.ReadLines(filename)
                                 .FirstOrDefault(line => line.StartsWith("// Assert result"));
            Assert.NotNull(assertLine);
            
            var match = Regex.Match(assertLine, @"\/\/ Assert result (\d+)");
            Assert.True(match.Success);

            var expected = int.Parse(match.Groups[1].Value);
            Assert.Equal(expected, result);
        }
    }

    // TODO: Mock it with Moq
    internal class Logger : ILogger
    {
        public Logger()
        {
        }

        public void Error(string message)
        {            
        }

        public void Error(Mono.ILASM.Location location, string message)
        {
        }

        public void Info(string message)
        {         
        }

        public void Warning(string message)
        {
        }

        public void Warning(Mono.ILASM.Location location, string message)
        {
        }
    }
}
