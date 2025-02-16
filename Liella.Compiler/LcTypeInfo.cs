using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
    public class LcTypeContext {
        public IReadOnlyDictionary<ITypeEntry, LcTypeInfo> NativeTypeMap => m_NativeTypeMap;
        protected Dictionary<ITypeEntry, LcTypeInfo> m_NativeTypeMap = new();

        public LcTypeContext(TypeEnvironment typeEnv) {

        }
    }
    public abstract class LcTypeInfo {
        public LcTypeContext Context { get; }
        public abstract ICGenType? VirtualTableType { get; }
        public abstract ICGenType GetInstanceTypeEnsureDef();
        public abstract ICGenNamedStructType GetDataStorageTypeEnsureDef();
        public abstract ICGenNamedStructType GetStaticStorageTypeEnsureDef();

        public abstract void SetupTypes();

        public LcTypeInfo(LcTypeContext context) {
            Context = context;
        }
    }
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
    public class LcTypeDefInfo : LcTypeInfo {
        protected Dictionary<FieldDefEntry, (int offset, int index)> m_DataStorageLayout = new();
        protected Dictionary<MethodDefEntry, LcMethodInfo> m_Methods = new();
        public ITypeEntry Entry { get; }
        public ICGenNamedStructType StaticStorage { get; }
        public ICGenNamedStructType DataStorage { get; }
        public ICGenType? InstanceType { get; protected set; }
        public override ICGenType? VirtualTableType => throw new NotImplementedException();
        public IReadOnlyDictionary<FieldDefEntry, (int offset, int index)> DataStorageLayout => m_DataStorageLayout;
        public IReadOnlyDictionary<MethodDefEntry, LcMethodInfo> Methods => m_Methods;
        public LayoutKind Layout { get; }
        public bool IsTypeDefined { get; protected set; }
        public CodeGenContext CgContext { get; }
        
        public LcTypeDefInfo(ITypeEntry entry, LcTypeContext typeContext, CodeGenContext cgContext) : base(typeContext) {
            Entry = entry;

            StaticStorage = cgContext.TypeFactory.CreateStruct($"static.{entry.FullName}");
            DataStorage = cgContext.TypeFactory.CreateStruct($"data.{entry.FullName}");

            var layoutAttribute = Entry.CustomAttributes
                .Where(e => e.ctor.DeclType.FullName == ".System.Runtime.InteropServices.StructLayoutAttribute")
                .Select(e => (LayoutKind)e.arguments.FixedArguments[0].Value!)
                .FirstOrDefault(Entry.IsValueType ? LayoutKind.Sequential : LayoutKind.Auto);

            if(Entry.Attributes.HasFlag(TypeAttributes.ExplicitLayout)) {
                Layout = LayoutKind.Explicit;
            } else {
                Layout = layoutAttribute;
            }

            CgContext = cgContext;
        }
        public override ICGenType GetInstanceTypeEnsureDef() {
            if(!IsTypeDefined) SetupTypes();
            return InstanceType!;
        }
        public override ICGenNamedStructType GetDataStorageTypeEnsureDef() {
            if(!IsTypeDefined) SetupTypes();
            return DataStorage;
        }
        public override ICGenNamedStructType GetStaticStorageTypeEnsureDef() {
            if(!IsTypeDefined) SetupTypes();
            return StaticStorage;
        }
        public override void SetupTypes() {
            if(IsTypeDefined) return;
            IsTypeDefined = true;

            StaticStorage.SetStructBody(
                Entry.TypeFields
                .Where(e => e.Attributes.HasFlag(FieldAttributes.Static))
                .Select(e => Context.NativeTypeMap[e.FieldType].GetInstanceTypeEnsureDef()).ToArray());

            var dataStorageElements = new List<ICGenType>();

            if(Layout == LayoutKind.Explicit) {
                throw new NotImplementedException();
            } else {
                var dataStorageTypes = new List<(FieldDefEntry field, ICGenType type)>();
                var baseStorage = Entry.BaseType is not null ? Context.NativeTypeMap[Entry.BaseType].GetDataStorageTypeEnsureDef(): null;

                foreach(var i in Entry.TypeFields) {
                    if(i.Attributes.HasFlag(FieldAttributes.Static)) continue;
                    dataStorageTypes.Add((i, Context.NativeTypeMap[i.FieldType].GetInstanceTypeEnsureDef()));
                }

                if(Layout == LayoutKind.Auto)
                    LcTypeLayoutOptimizer.OptimizeLayout(baseStorage, dataStorageTypes);

                var types = dataStorageTypes.Select(e => e.type);
                DataStorage.SetStructBody(baseStorage is null ? types.ToArray() : types.Prepend(baseStorage).ToArray());
            }

            InstanceType = Entry.IsValueType ? DataStorage : CgContext.TypeFactory.CreatePointer(DataStorage);


            var currentEntry = Entry;

        }

    }
}
