using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Test {
    public static class CompilationHelpers {
        public static MemoryStream CompileSource(string source, string systemFrameworkPath) {
            var tree = CSharpSyntaxTree.ParseText(source);

            var imageStream = new MemoryStream();
            var framework = MetadataReference.CreateFromFile(systemFrameworkPath);
            var compilation = CSharpCompilation.Create("test1", [tree], [framework], options: new(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            var result = compilation.Emit(imageStream);
            imageStream.Seek(0, SeekOrigin.Begin);

            return imageStream;
        }
    }
}
