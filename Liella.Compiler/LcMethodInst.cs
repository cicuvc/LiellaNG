using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {

    public class LcMethodInst :LcMethodInfo{
        protected FrozenDictionary<ITypeEntry, ITypeEntry> m_GenericSubstituteMap;
        public MethodInstantiation MethodInstantiation { get; }
        
        public LcMethodInst(LcTypeInfo declType,MethodInstantiation methodInst, LcCompileContext context, CodeGenContext cgContext) : base(declType,methodInst.InvariantPart.Definition,context,cgContext) {
            MethodInstantiation = methodInst;

            m_GenericSubstituteMap = methodInst.FormalArguments.Zip(methodInst.ActualArguments).ToFrozenDictionary(e => e.First, e => e.Second);
        }


        protected override void InitializeFunction() {
            var argumentsType = MethodInstantiation.Signature.ParameterTypes.Select(e => ResolveContextType(e).GetInstanceTypeEnsureDef()).ToImmutableArray();
            var returnType = ResolveContextType(MethodInstantiation.Signature.ReturnType).GetInstanceTypeEnsureDef();


            var hasImpl = !MethodInstantiation.Attributes.HasFlag(MethodAttributes.Abstract);
            m_MethodType = CgContext.TypeFactory.CreateFunction(argumentsType.AsSpan(), returnType);
            m_MethodFunction = CgContext.CreateFunction(MethodInstantiation.FullName, m_MethodType, hasImpl);
        }
        protected override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            var subType = GenericSubstitutionHelpers.SubstituteGenericEntry(Context.TypeEnv.EntryManager, m_GenericSubstituteMap, entry);
            return base.ResolveContextType(subType);
        }
    }
}
