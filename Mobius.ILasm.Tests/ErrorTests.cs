using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mobius.ILasm.Core;
using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using Mono.ILASM;
using Moq;
using Xunit;

namespace Mobius.ILasm.Tests
{
    public class ErrorTests
    {
        [Fact]
        public void Duplicate_ClassMethod_IsReportedy()
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
        public void Duplicate_TopLevelMethod_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .method void Test() cil managed {}
                .method void Test() cil managed {}
            ");

            Assert.Single(errors, "Duplicate method declaration: instance System.Void Test()");
        }

        [Fact]
        public void Duplicate_Field_IsReported()
        {
            var errors = AssembleAndGetErrors(@"
                .class C
                    extends System.Object
                {
                    .field int32 f
                    .field int32 f
                }
            ");

            Assert.Single(errors, "Duplicate field declaration: System.Int32 f");
        }

        [Fact]
        public void MissingManifestResource_IsReported()
        {
            var errors = AssembleAndGetErrors(@".mresource public NoSuchFile.txt {}");

            Assert.Single(errors, $"Resource file 'NoSuchFile.txt' was not found");
        }

        private static IReadOnlyList<string> AssembleAndGetErrors(string il)
        {
            var logger = new Mock<ILogger>();
            var driver = new Driver(logger.Object, Driver.Target.Dll);

            driver.Assemble(new[] { il }, new MemoryStream());
            return logger.Invocations
                .Where(i => i.Method.Name == nameof(ILogger.Error))
                .Select(i => (string)i.Arguments.Last())
                .ToList();
        }
    }
}
