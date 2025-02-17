using Liella.Backend.Components;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;

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
        public TypeInstantiationEntry TypeInstantiation { get; }
        public LcTypeInstInfo(TypeInstantiationEntry instEntry,TypeDefEntry defEntry, LcCompileContext typeContext, CodeGenContext cgContext) : base(defEntry, typeContext, cgContext) {
            TypeInstantiation = instEntry;
        }
        // [TODO] Support type substitute
        protected override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            var realTypeEntry = entry;
            if(entry is GenericPlaceholderTypeEntry placeholder) {
                var placeholderIndex = Entry.TypeArguments.IndexOf(placeholder);
                if(placeholderIndex < 0) throw new ArgumentException("Missing placeholder ???");

                realTypeEntry = TypeInstantiation.InvariantPart.TypeArguments[placeholderIndex];
            }
            return base.ResolveContextType(realTypeEntry);
        }
    }
}
