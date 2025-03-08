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
        public MethodDefEntry MethodDefinitionEntry { get; }
        
        public LcMethodInst(LcTypeInfo declType,MethodInstantiation methodInst, LcCompileContext context, CodeGenContext cgContext) : base(declType,methodInst,context,cgContext) {
            MethodDefinitionEntry = methodInst.InvariantPart.Definition;

            m_GenericSubstituteMap = methodInst.FormalArguments.Zip(methodInst.ActualArguments).ToFrozenDictionary(e => e.First, e => e.Second);
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
