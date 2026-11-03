using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace NuciWeb.Steam.ArchitectureTests
{
    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        [Test]
        public void ProductionSourceFiles_ContainOneTypeMatchingTheFileName()
        {
            var sourceDirectory = Path.Combine(AppContext.BaseDirectory, "ProductionSource");
            var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(sourceFiles, Is.Not.Empty, "Production source files were not copied to the test output.");

            foreach (var sourceFile in sourceFiles)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile));
                var declarations = syntaxTree.GetRoot().DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Concat<SyntaxNode>(syntaxTree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>())
                    .ToArray();
                var identifiers = declarations.Select(GetIdentifier).ToArray();
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);

                Assert.That(identifiers, Has.Length.EqualTo(1),
                    $"{sourceFile} must contain exactly one declared type.");
                Assert.That(identifiers[0], Is.EqualTo(fileName),
                    $"{sourceFile} must contain a type named {fileName}.");
            }
        }

        private static string GetIdentifier(SyntaxNode declaration)
        {
            return declaration switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
                _ => string.Empty
            };
        }
    }
}
