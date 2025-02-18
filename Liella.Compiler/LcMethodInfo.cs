using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public class LcMethodInfo {
        public  LcCompileContext Context { get; }
        public CodeGenContext CgContext { get; }
        public LcTypeInfo DeclType { get; }
        public IMethodEntry Entry { get; }
        public bool IsStatic { get; }
        public bool IsVirtualOverride { get; }
        public bool IsVirtualDef { get; }
        public ILMethodAnalyzer? ILCodeAnalyzer { get; }
        public ICGenFunctionType MethodType { get; }
        public CodeGenFunction MethodFunction { get; }
        public LcMethodInfo(LcTypeInfo type, IMethodEntry entry,LcCompileContext context, CodeGenContext cgContext) {
            Context = context;
            CgContext = cgContext;

            DeclType = type;
            Entry = entry;

            var methodDef = entry is MethodDefEntry defEntry ? defEntry : (MethodDefEntry)((MethodInstantiation)entry).Definition;

            IsStatic = methodDef.Attriutes.HasFlag(MethodAttributes.Static);
            IsVirtualOverride = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && !methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);
            IsVirtualDef = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);

            if(!entry.Attributes.HasFlag(MethodAttributes.Abstract))
                ILCodeAnalyzer = new(entry.Decoder);

            var argumentsType = entry.Signature.ParameterTypes.Select(e => ResolveContextType(e).GetInstanceTypeEnsureDef()).ToImmutableArray();
            var returnType = ResolveContextType(entry.Signature.ReturnType).GetInstanceTypeEnsureDef();

            var hasImpl = !entry.Attributes.HasFlag(MethodAttributes.Abstract);
            MethodType = cgContext.TypeFactory.CreateFunction(argumentsType.AsSpan(), returnType);
            MethodFunction = cgContext.CreateFunction(entry.FullName, MethodType, hasImpl);
        }
        protected virtual LcTypeInfo ResolveContextType(ITypeEntry entry) {
            return DeclType.ResolveContextType(entry)!;
        }
        public void GenerateDecl() {
            
        }
        public void GenerateCode() {

        }
    }
}
