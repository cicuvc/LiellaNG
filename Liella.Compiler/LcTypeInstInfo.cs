using Liella.Backend.Components;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System.Collections.Frozen;
using System.Collections.Immutable;

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
        
        public LcTypeInstInfo(TypeInstantiationEntry instEntry,TypeDefEntry defEntry, LcCompileContext typeContext, CodeGenContext cgContext) : base(defEntry, typeContext, cgContext) {
            TypeInstantiation = instEntry;

            m_GenericSubstitutionMap = instEntry.FormalArguments.Zip(instEntry.ActualArguments).ToFrozenDictionary(e => e.First, e => e.Second);
        }
        // [TODO] Support type substitute
        public override LcTypeInfo? ResolveContextType(ITypeEntry entry) {
            var realTypeEntry = SubstituteGenericEntry(entry);
            return base.ResolveContextType(realTypeEntry);
        }

        protected ITypeEntry SubstituteGenericEntry(ITypeEntry entry, bool likelyPrimaryInst = true) {
            if(entry is TypeDefEntry) return entry;
            if(entry is GenericPlaceholderTypeEntry placeholder) {
                return m_GenericSubstitutionMap[entry];
            }
            if(entry is PointerTypeEntry pointer) {
                return PointerTypeEntry.Create(Context.TypeEnv.EntryManager, SubstituteGenericEntry(pointer.InvariantPart.ElementType, likelyPrimaryInst));
            }
            if(entry is ReferenceTypeEntry reference) {
                return PointerTypeEntry.Create(Context.TypeEnv.EntryManager, SubstituteGenericEntry(reference.InvariantPart.ElementType, likelyPrimaryInst));
            }
            if(entry is TypeInstantiationEntry typeInst) {
                var typeArguments = typeInst.InvariantPart.TypeArguments.Select(e=> SubstituteGenericEntry(e,false)).ToImmutableArray();
                return TypeInstantiationEntry.Create(Context.TypeEnv.EntryManager, typeInst.InvariantPart.DefinitionType, typeArguments, likelyPrimaryInst);
            }
            return entry;
        }
    }
}
