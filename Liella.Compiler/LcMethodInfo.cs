using Liella.Backend.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public class LcMethodInfo {
        public LcTypeInfo DeclType { get; }
        public IMethodEntry Entry { get; }
        public bool IsStatic { get; }
        public bool IsVirtualOverride { get; }
        public bool IsVirtualDef { get; }
        public LcMethodInfo(LcTypeInfo type, IMethodEntry entry) {
            DeclType = type;
            Entry = entry;

            var methodDef = entry is MethodDefEntry defEntry ? defEntry : (MethodDefEntry)((MethodInstantiation)entry).Definition;

            IsStatic = methodDef.Attriutes.HasFlag(MethodAttributes.Static);
            IsVirtualOverride = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && !methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);
            IsVirtualDef = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);
        }
    }
}
