using Liella.Backend.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public static class GenericSubstitutionHelpers {
        
        public static ITypeEntry SubstituteGenericEntry(EntityEntryManager manager,IReadOnlyDictionary<ITypeEntry, ITypeEntry> genericMap, ITypeEntry entry, bool likelyPrimaryInst = true) {
            if(entry is TypeDefEntry) return entry;
            if(entry is IGenericPlaceholder placeholder) {
                return genericMap.GetValueOrDefault(entry, entry);
            }
            if(entry is PointerTypeEntry pointer) {
                return PointerTypeEntry.Create(manager, SubstituteGenericEntry(manager, genericMap, pointer.InvariantPart.ElementType, likelyPrimaryInst));
            }
            if(entry is ReferenceTypeEntry reference) {
                return PointerTypeEntry.Create(manager, SubstituteGenericEntry(manager, genericMap, reference.InvariantPart.ElementType, likelyPrimaryInst));
            }
            if(entry is TypeInstantiationEntry typeInst) {
                var typeArguments = typeInst.InvariantPart.TypeArguments.Select(e => SubstituteGenericEntry(manager, genericMap, e, false)).ToImmutableArray();
                return TypeInstantiationEntry.Create(manager, typeInst.InvariantPart.DefinitionType, typeArguments, likelyPrimaryInst);
            }
            return entry;
        }
    }
}
