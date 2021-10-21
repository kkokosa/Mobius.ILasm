using System.Collections.Generic;
using System.IO;
using Mobius.ILasm.Core;
using Mobius.ILasm.interfaces;
using Mono.ILASM;
using Moq;
using Xunit;

namespace Mobius.ILasm.Tests
{
    public class ErrorTests
    {
        [Fact]
        public void Duplicate_ClassMethod_IsReportedCorrectly()
        {
            var errors = AssembleAndGetErrors(@"
                .class C
                    extends System.Object
                {
                    .method void Test() cil managed {}
                    .method void Test() cil managed {}
                }
            ");

            Assert.Single(errors, "Duplicate method declaration: instance System.Void Test()");
        }

        [Fact]
        public void Duplicate_TopLevelMethod_IsReportedCorrectly()
        {
            var errors = AssembleAndGetErrors(@"
                .method void Test() cil managed {}
                .method void Test() cil managed {}
            ");

            Assert.Single(errors, "Duplicate method declaration: instance System.Void Test()");
        }

        private static IReadOnlyList<string> AssembleAndGetErrors(string il)
        {
            var logger = new Mock<ILogger>();
            var errors = new List<string>();
            logger.Setup(l => l.Error(It.IsAny<Location>(), Capture.In(errors)));

            var driver = new Driver(logger.Object, Driver.Target.Dll, false, false, false);

            driver.Assemble(new[] { il }, new MemoryStream());
            return errors;
        }
    }
}
