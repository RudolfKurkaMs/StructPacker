using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RudolfKurka.StructPacker
{
    [Generator]
    public class StructPackerGenerator : ISourceGenerator
    {
        private class MainReceiver : ISyntaxReceiver
        {
            public List<StructDeclarationSyntax> FoundItems { get; } = new List<StructDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is StructDeclarationSyntax decSyntax)
                    FoundItems.Add(decSyntax);
            }
        }

        /// <summary>
        /// Called before generation occurs. A generator can use the <paramref name="ctx" />
        /// to register callbacks required to perform generation.
        /// </summary>
        /// <param name="ctx">The <see cref="T:Microsoft.CodeAnalysis.GeneratorInitializationContext" /> to register callbacks on</param>
        public void Initialize(GeneratorInitializationContext ctx)
        {
            ctx.RegisterForSyntaxNotifications(() => new MainReceiver());
        }

        /// <summary>
        /// Called to perform source generation. A generator can use the <paramref name="ctx" />
        /// to add source files via the <see cref="M:Microsoft.CodeAnalysis.GeneratorExecutionContext.AddSource(System.String,Microsoft.CodeAnalysis.Text.SourceText)" />
        /// method.
        /// </summary>
        /// <param name="ctx">The <see cref="T:Microsoft.CodeAnalysis.GeneratorExecutionContext" /> to add source to</param>
        /// <remarks>
        /// This call represents the main generation step. It is called after a <see cref="T:Microsoft.CodeAnalysis.Compilation" /> is
        /// created that contains the user written code.
        /// A generator can use the <see cref="P:Microsoft.CodeAnalysis.GeneratorExecutionContext.Compilation" /> property to
        /// discover information about the users compilation and make decisions on what source to
        /// provide.
        /// </remarks>
        public void Execute(GeneratorExecutionContext ctx)
        {
            var receiver = (MainReceiver)ctx.SyntaxReceiver;

            if (receiver == null || receiver.FoundItems.Count == 0)
                return;

            try
            {
                SourceText extensionsText = SourceText.From(GenerateSourceFile(receiver.FoundItems, ctx), Encoding.UTF8);
                ctx.AddSource("structpacker_extensions.cs", extensionsText);
            }
            catch (Exception e)
            {
                var diag = Diagnostic.Create("RKSPE", "StructPacker", $"StructPacker: {e.Message}", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0);
                ctx.ReportDiagnostic(diag);
            }
        }

        private static string GenerateSourceFile(IEnumerable<StructDeclarationSyntax> inputClasses, GeneratorExecutionContext ctx)
        {
            var code = new CodeGen();

            code.AppendUsings("StructPacker");
            code.Line();

            using (code.CodeBlock("internal static class struct_packer_generated"))
            {
                foreach (StructDeclarationSyntax declaration in inputClasses)
                    GenerateClass(declaration, code, ctx);
            }

            return code.ToString();
        }

        private static readonly SymbolDisplayFormat TypeNameFormat = new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> source, string fullName, SemanticModel model, GeneratorExecutionContext ctx)
        {
            return source.SelectMany(list => list.Attributes).Any(
                atr =>
                {
                    TypeInfo typeInfo = model.GetTypeInfo(atr, ctx.CancellationToken);
                    string typeName = typeInfo.Type?.ToDisplayString(TypeNameFormat);

                    return string.Equals(typeName, fullName, StringComparison.Ordinal);
                });
        }

        private static void GenerateClass(TypeDeclarationSyntax decSyntax, CodeGen code, GeneratorExecutionContext ctx)
        {
            SemanticModel model = ctx.Compilation.GetSemanticModel(decSyntax.SyntaxTree);

            if (HasAttribute(decSyntax.AttributeLists, "StructPacker.PackAttribute", model, ctx))
            {
                INamedTypeSymbol decSymb = model.GetDeclaredSymbol(decSyntax);
                string classFqn = decSymb?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (string.IsNullOrWhiteSpace(classFqn))
                    return;

                var fields = new List<string>();

                foreach (PropertyDeclarationSyntax p in decSyntax.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (HasAttribute(p.AttributeLists, "StructPacker.SkipPackAttribute", model, ctx))
                        continue;

                    if (p.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword))
                        continue;

                    AccessorDeclarationSyntax getter = p.AccessorList?.Accessors.FirstOrDefault(x => x.IsKind(SyntaxKind.GetAccessorDeclaration));
                    AccessorDeclarationSyntax setter = p.AccessorList?.Accessors.FirstOrDefault(x => x.IsKind(SyntaxKind.SetAccessorDeclaration));

                    if (getter == null || setter == null)
                        throw new Exception($"Type \"{classFqn}\" has properties with missing \"get\" or \"set\" accessors and cannot be serialized. Add missing accessors or skip serializing this property with \"SkipPack\" attribute if this is intentional.");

                    if (getter.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword) || setter.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword))
                        throw new Exception($"Type \"{classFqn}\" has properties with \"get\" or \"set\" accessors marked as private and cannot be serialized. Remove private modifier or skip serializing this property with \"SkipPack\" attribute if this is intentional.");
                    
                    fields.Add(p.Identifier.Text);
                }

                foreach (FieldDeclarationSyntax p in decSyntax.Members.OfType<FieldDeclarationSyntax>())
                {
                    if (HasAttribute(p.AttributeLists, "StructPacker.SkipPackAttribute", model, ctx))
                        continue;

                    if (p.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword))
                        continue;
                    
                    foreach (VariableDeclaratorSyntax varSyn in p.Declaration.Variables)
                        fields.Add(varSyn.Identifier.Text);
                }

                GenerateExtensions(classFqn, fields, code);
            }
        }

        private static readonly string
            FqnPooledBuffer = "PooledBuffer",
            FqnTools = "Tools",
            FqnStream = typeof(Stream).FullName,
            FqnByteArr = typeof(byte[]).FullName;
        
        private static void GenerateExtensions(string className, List<string> fields, CodeGen code)
        {
            if (fields.Count == 0)
                throw new Exception($"Type \"{className}\" does not contain any valid members. Serializing empty types is meaningless as they take zero bytes. Add some members or exclude this type from serialization.");
            
            // fields.Sort(CompareTwoFields);

            code.AppendIndent().Append($"private static int GetSize(ref {className} msg) => 0");

            foreach (string id in fields)
                code.Append($" + {FqnTools}.GetSize(msg.{id})");

            code.Append(";").Line();

            code.Line("/// <summary>");
            code.Line("/// Deserializes content from a byte array into the provided structure.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to deserialize into.</param>");
            code.Line("/// <param name=\"sourceData\">Buffer to read from.</param>");
            code.Line("/// <param name=\"startIndex\">Optional index to start reading the buffer from.</param>");
            code.Line($"public static void Unpack(this ref {className} msg, {FqnByteArr} sourceData, int startIndex = 0) => {FqnTools}.UnpackMsg(ref msg, sourceData, ref startIndex, ReadPropsFromBytes);");
           
            code.Line("/// <summary>");
            code.Line("/// Deserializes content from a byte array into the provided structure.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to deserialize into.</param>");
            code.Line("/// <param name=\"sourceData\">Buffer to read from.</param>");
            code.Line("/// <param name=\"startIndex\">Index to start reading the buffer from that will be incremented as data are read.</param>");
            code.Line($"public static void Unpack(this ref {className} msg, {FqnByteArr} sourceData, ref int startIndex) => {FqnTools}.UnpackMsg(ref msg, sourceData, ref startIndex, ReadPropsFromBytes);");
            
            code.Line("/// <summary>");
            code.Line("/// Deserializes content from a stream into the provided structure.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to deserialize into.</param>");
            code.Line("/// <param name=\"sourceStream\">Stream to read from.</param>");
            code.Line($"public static void Unpack(this ref {className} msg, {FqnStream} sourceStream) => {FqnTools}.UnpackMsg(ref msg, sourceStream, ReadPropsFromStream);");
            
            code.Line("/// <summary>");
            code.Line("/// Serializes provided structure into a stream.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to serialize.</param>");
            code.Line("/// <param name=\"destinationStream\">Target stream to write into.</param>");
            code.Line($"public static void Pack(this ref {className} msg, {FqnStream} destinationStream) => {FqnTools}.PackMsgToStream(ref msg, destinationStream, GetSize(ref msg), WriteProps);");
           
            code.Line("/// <summary>");
            code.Line("/// Serializes provided structure into a byte array.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to serialize.</param>");
            code.Line("/// <returns>Resulting byte array that contains the serialized structure.</returns>");
            code.Line($"public static {FqnByteArr} Pack(this ref {className} msg) => {FqnTools}.PackMsgToArray(ref msg, GetSize(ref msg), WriteProps);");
           
            code.Line("/// <summary>");
            code.Line("/// Serializes provided structure into a pooled memory buffer.");
            code.Line("/// </summary>");
            code.Line("/// <param name=\"msg\">Instance to serialize.</param>");
            code.Line("/// <returns>Disposable memory buffer that contains the serialized structure.</returns>");
            code.Line($"public static {FqnPooledBuffer} PackToBuffer(this ref {className} msg) => {FqnTools}.PackMsgToBuffer(ref msg, GetSize(ref msg), WriteProps);");

            using (code.CodeBlock($"private static void ReadPropsFromStream(ref {className} msg, {FqnStream} srcStream, {FqnByteArr} gpBuffer)"))
            {
                foreach (string id in fields)
                    code.Line($"msg.{id} = {FqnTools}.ReadFromStream(msg.{id}, srcStream, gpBuffer);");
            }

            using (code.CodeBlock($"private static void ReadPropsFromBytes(ref {className} msg, {FqnByteArr} srcBytes, ref int startIndex)"))
            {
                foreach (string id in fields)
                    code.Line($"msg.{id} = {FqnTools}.ReadFromBytes(msg.{id}, srcBytes, ref startIndex);");
            }

            using (code.CodeBlock($"private static void WriteProps(ref {className} msg, byte[] destBytes, ref int startIndex)"))
            {
                foreach (string id in fields)
                    code.Line($"{FqnTools}.Write(msg.{id}, destBytes, ref startIndex);");
            }
        }

        // private static int CompareTwoFields(string left, string right) => string.Compare(left, right, StringComparison.Ordinal);
    }
}