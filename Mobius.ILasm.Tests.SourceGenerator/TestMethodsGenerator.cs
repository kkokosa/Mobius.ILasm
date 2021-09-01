using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.IO;

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

        private const string testMethodText = @"
        [Fact]
        public void Test_helloworldconsole() 
            => AssembleAndVerify("");    
";

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("GenerateTestMethodsAttribute.cs", SourceText.From(testGeneratorAttribute, Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver)) return;

            CSharpParseOptions options = (CSharpParseOptions)((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;

            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(testGeneratorAttribute, Encoding.UTF8), options));

            foreach (var candidateTypeNode in receiver.Candidates)
            {
                var model = compilation.GetSemanticModel(candidateTypeNode.Item1.SyntaxTree);
                var testFileName = GetFilenames(candidateTypeNode.Item2);

            }

            //var syntaxTrees = context.Compilation.SyntaxTrees;
            //var testClasses = syntaxTrees.Where(x => x.GetText().ToString().Contains("[GenerateTestMethods"));

            //foreach (var testClass in testClasses)
            //{
            //    var usingDirectives = testClass.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>();
            //    var usingDirectiveAsText = string.Join("\r\n", usingDirectives);
            //    var customTestAttribute = testClass.GetRoot().DescendantNodes().OfType<AttributeSyntax>()
            //        .FirstOrDefault(x => x.Name.To == "GenerateTestMethodsAttribute");
            //    //
            //}
        }

        private object GetFilenames(string item2)
        {
            //Error below when debugging as despite a valid path, I get illegal characters in path in the exception.
            //var directory = new DirectoryInfo("");
            //var files = directory.GetFiles(item2);
            return 0;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<(ClassDeclarationSyntax, string)> Candidates { get; } = new List<(ClassDeclarationSyntax, string)>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                foreach (var attributeList in classDeclarationSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString() == "GenerateTestMethods")
                        {
                            this.Candidates.Add((classDeclarationSyntax, attribute.ArgumentList.Arguments[0].ToString()));
                        }
                    }
                }
            }
        }
    }
}
