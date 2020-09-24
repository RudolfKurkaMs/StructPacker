using System;
using System.Text;

namespace RudolfKurka.StructPacker
{
    internal sealed class CodeGen
    {
        private const char TabChar = '\t';
        private readonly string _lineTerminator = Environment.NewLine;
        private readonly StringBuilder _str = new StringBuilder();
        private int _indent;
        
        private string Indent => new string(' ', _indent * 4);

        public void AppendUsings(params string[] items)
        {
            foreach (string item in items)
                Line("using ", item, ";");
        }

        public DisposableCallback NamespaceBlock(string name)
        {
            BeginNamespace(name);
            return new DisposableCallback(EndNamespace);
        }

        public void BeginNamespace(string name)
        {
            Line("namespace ", name);
            BeginCodeBlock();
        }

        public void EndNamespace() => EndCodeBlock();

        public DisposableCallback CheckedBlock(bool @unchecked = false)
        {
            BeginChecked(@unchecked);
            return new DisposableCallback(EndChecked);
        }

        public void BeginChecked(bool @unchecked = false)
        {
            Line(@unchecked ? "unchecked" : "checked");
            BeginCodeBlock();
        }

        public void EndChecked() => EndCodeBlock();

        public DisposableCallback CodeBlock(string line = null)
        {
            if (line != null)
                Line(line);

            BeginCodeBlock();
            return new DisposableCallback(EndCodeBlock);
        }

        public void BeginCodeBlock()
        {
            Line("{");
            IncreaseIndent();
        }

        public void EndCodeBlock()
        {
            DecreaseIndent();
            Line("}");
        }

        public DisposableCallback IndentBlock()
        {
            IncreaseIndent();
            return new DisposableCallback(DecreaseIndent);
        }

        public void IncreaseIndent() => _indent++;

        public void DecreaseIndent() => _indent--;

        public DisposableCallback RegionBlock(string name)
        {
            BeginRegion(name);
            return new DisposableCallback(EndRegion);
        }

        public void BeginRegion(string name) => Line("#region ", name);

        public void EndRegion() => Line("#endregion");

        public CodeGen AppendIndent()
        {
            _str.Append(Indent);
            return this;
        }

        public CodeGen Append(params string[] texts)
        {
            _str.Append(string.Concat(texts));
            return this;
        }

        public CodeGen Line(params string[] line)
        {
            _str.Append(Indent).Append(string.Concat(line)).Append(_lineTerminator);
            return this;
        }

        public static string Tab(int count = 1) => new string(TabChar, count);

        public override string ToString() => _str.ToString();
    }
}