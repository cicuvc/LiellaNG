using Liella.Backend.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public class LcMethodInfo {
        public LcTypeInfo DeclType { get; }
        public IMethodEntry Entry { get; }
        public LcMethodInfo(LcTypeInfo type, IMethodEntry entry) {
            DeclType = type;
            Entry = entry;
        }
    }
}
