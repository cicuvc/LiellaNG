using Liella.Backend.Types;
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
    public static class LcTypeLayoutOptimizer { 
        public static void OptimizeLayout(ICGenType? baseStorage, List<(FieldDefEntry field, ICGenType type)> types) {
            var typeGroups = types.GroupBy(e => e.type.Alignment)
                .Select(e => (align: e.Key, value: e.ToList())).ToList();

            typeGroups.Sort((u, v) => -u.align.CompareTo(v.align));
            foreach(var i in typeGroups) {
                i.value.Sort((u, v) => -u.type.Alignment.CompareTo(v.type.Alignment));
            }

            var currentOffset = baseStorage?.Size ?? 0;
            for(var i=0;i< types.Count; i++) {
                var candidate = default((FieldDefEntry field, ICGenType type));
                while(candidate.type is null) {
                    for(var j = 0; j < typeGroups.Count; j++) {
                        if(currentOffset % typeGroups[j].align != 0) continue;
                        var typeSet = typeGroups[j].value;
                        var lastElement = typeSet.Count - 1;
                        candidate = typeSet[lastElement];
                        typeSet.RemoveAt(lastElement);

                        if(typeSet.Count == 0) typeGroups.RemoveAt(j);

                        break;
                    }
                    if(candidate.type is null) {
                        var alignment = typeGroups.Last().align;
                        currentOffset = (currentOffset + alignment - 1) / alignment * alignment;
                    }
                }
                currentOffset += candidate.type.Size;
                types[i] = candidate;
            }
        }
    }
}
