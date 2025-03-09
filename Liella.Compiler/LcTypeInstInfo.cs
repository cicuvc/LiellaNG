using Liella.Backend.Components;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

/*
 * A CLR type is mapped into 5 native types
 * Static storage (S) => a struct containing all unique static fields
 * Data Storage Type (D) => a struct containing all instance fields
 * Reference Type (R) => type for object on heap ([V*, D])
 * 
 * Instance Type (I) => (R) for reference type, (D*) for value type
 * ---------------------------------------------------------------------
 * 
 * Reference type can be only stored on heap, -> [V, D]
 * Value type can on stack/register/heap
 * When value types not boxed, instance type of it 
 */

namespace Liella.Backend.Compiler {
    public class LcTypeInstInfo : LcTypeDefInfo {
        protected FrozenDictionary<ITypeEntry, ITypeEntry> m_GenericSubstitutionMap;
        public TypeInstantiationEntry TypeInstantiation { get; }
        public override ITypeEntry ExactEntry => TypeInstantiation;

        public LcTypeInstInfo(TypeInstantiationEntry instEntry,TypeDefEntry defEntry, LcCompileContext typeContext, CodeGenContext cgContext) : base(defEntry, typeContext, cgContext) {
            TypeInstantiation = instEntry;

            m_GenericSubstitutionMap = instEntry.FormalArguments.Zip(instEntry.ActualArguments).ToFrozenDictionary(e => e.First, e => e.Second);
        }
        // [TODO] Support type substitute
        public override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            var realTypeEntry = GenericSubstitutionHelpers.SubstituteGenericEntry(Context.TypeEnv.EntryManager, m_GenericSubstitutionMap,entry);
            return base.ResolveContextType(realTypeEntry);
        }

        public override LcMethodInfo ResolveContextMethod(IMethodEntry entry) {
            if(entry is MethodDefEntry methodDef) {
                if(methodDef.DeclType == Entry) {
                    entry = MethodInstantiation.Create(Context.TypeEnv.EntryManager, ExactEntry, methodDef, []);
                    Debugger.Break();
                }
            }

            return base.ResolveContextMethod(entry);
        }
    }
}
