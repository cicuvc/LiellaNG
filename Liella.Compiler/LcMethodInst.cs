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
        public MethodInstantiation MethodInstantiationEntry { get; }
        
        public LcMethodInst(LcTypeInfo declType,MethodInstantiation methodInst, LcCompileContext context, CodeGenContext cgContext) : base(declType,methodInst.InvariantPart.Definition,context,cgContext) {
            MethodInstantiationEntry = methodInst;

            m_GenericSubstituteMap = methodInst.FormalArguments.Zip(methodInst.ActualArguments).ToFrozenDictionary(e => e.First, e => e.Second);
        }


        protected override void InitializeFunction() {
            var argumentsType = MethodInstantiationEntry.Signature.ParameterTypes.Select(e => ResolveContextType(e).GetInstanceTypeEnsureDef()).ToImmutableArray();
            var returnType = ResolveContextType(MethodInstantiationEntry.Signature.ReturnType).GetInstanceTypeEnsureDef();


            var hasImpl = !MethodInstantiationEntry.Attributes.HasFlag(MethodAttributes.Abstract);
            m_MethodType = CgContext.TypeFactory.CreateFunction(argumentsType.AsSpan(), returnType);
            m_MethodFunction = CgContext.CreateFunction(MethodInstantiationEntry.FullName, m_MethodType, hasImpl);
        }
        protected override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            var subType = GenericSubstitutionHelpers.SubstituteGenericEntry(Context.TypeEnv.EntryManager, m_GenericSubstituteMap, entry);
            return base.ResolveContextType(subType);
        }

        protected override LcMethodInfo ResolveVirtualPrototypeMethod() {
            if(IsVirtualDef) return this;
            if(!IsVirtualOverride) throw new NotSupportedException();

            var prototype = Entry.VirtualMethodPrototype;
            throw new NotImplementedException();
            //var instantiation = MethodInstantiation.Create(Context.TypeEnv.EntryManager, prototype.DeclType, )
            return Context.NativeMethodMap[prototype!];
        }
    }
}
