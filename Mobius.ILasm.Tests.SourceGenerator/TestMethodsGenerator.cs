using Microsoft.CodeAnalysis;
using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Mobius.ILasm.Tests.SourceGenerator
{
    [Generator]
    public class TestMethodsGenerator : ISourceGenerator
    {
        private const string testGeneratorAttribute = @"
using System;
namespace Mobius.ILasm.Tests.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class GenerateTestMethodsAttribute : Attribute
    {
        private string filePath;
        public GenerateTestMethodsAttribute(string filePath)
        {
            this.filePath = filePath;
        }
    }
}";

        public void Execute(GeneratorExecutionContext context)
        {
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(testGeneratorAttribute, Encoding.UTF8), options));
            context.AddSource("GenerateTestMethodsAttribute.cs", SourceText.From(testGeneratorAttribute, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {


        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {

        }
    }
}
