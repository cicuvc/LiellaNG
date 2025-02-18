using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System.Reflection;
using System.Runtime.InteropServices;

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
    public class LcTypeDefInfo : LcTypeInfo {
        protected Dictionary<FieldDefEntry, (int offset, int index)> m_DataStorageLayout = new();
        public override bool IsStorageRequired => true;
        public ICGenType? InstanceType { get; protected set; }
        public IReadOnlyDictionary<FieldDefEntry, (int offset, int index)> DataStorageLayout => m_DataStorageLayout;
        public LayoutKind Layout { get; }
        public CodeGenContext CgContext { get; }
        
        public LcTypeDefInfo(TypeDefEntry entry, LcCompileContext typeContext, CodeGenContext cgContext) : base(entry, typeContext) {
            m_StaticStorageType = cgContext.TypeFactory.CreateStruct($"static.{entry.FullName}");
            m_DataStorageType = cgContext.TypeFactory.CreateStruct($"data.{entry.FullName}");
            m_VTableType = cgContext.TypeFactory.CreateStruct($"vtable.{entry.FullName}");

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
        public override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            return Context.NativeTypeMap.GetValueOrDefault(entry,null!);
        }
        protected override ICGenNamedStructType SetupDataStorage() {
            var dataStorageElements = new List<ICGenType>();

            if(Layout == LayoutKind.Explicit) {
                throw new NotImplementedException();
            } else {
                var dataStorageTypes = new List<(FieldDefEntry field, ICGenType type)>();
                var baseStorage = Entry.BaseType is not null ? ResolveContextType(Entry.BaseType).GetDataStorageTypeEnsureDef() : null;

                foreach(var i in Entry.TypeFields) {
                    if(i.Attributes.HasFlag(FieldAttributes.Static)) continue;
                    dataStorageTypes.Add((i, ResolveContextType(i.FieldType).GetInstanceTypeEnsureDef()));
                }

                if(Layout == LayoutKind.Auto)
                    LcTypeLayoutOptimizer.OptimizeLayout(baseStorage, dataStorageTypes);

                var types = dataStorageTypes.Select(e => e.type);
                m_DataStorageType!.SetStructBody((baseStorage is null || Entry.IsValueType) ? types.ToArray() : types.Prepend(baseStorage).ToArray());
            }
            return m_DataStorageType;
        }
        protected override ICGenType SetupInstanceType() {
            if(Entry is TypeDefEntry typeDef) {
                if(typeDef.GetDetails().IsEnum) {
                    var fieldType = typeDef.GetField("value__").FieldType;
                    return Context.NativeTypeMap[fieldType].GetInstanceTypeEnsureDef();
                }
            }
            if(Entry.Attributes.HasFlag(TypeAttributes.Interface)) {
                return CgContext.TypeFactory.CreateStruct([CgContext.TypeFactory.VoidPtr, CgContext.TypeFactory.CreatePointer(m_VTableType!)]);
            }
            return Entry.IsValueType ? m_DataStorageType! : CgContext.TypeFactory.CreatePointer(m_DataStorageType!);
        }
        protected override ICGenNamedStructType SetupStaticStorage() {
            m_StaticStorageType!.SetStructBody(
               Entry.TypeFields
               .Where(e => e.Attributes.HasFlag(FieldAttributes.Static))
               .Select(e => Context.NativeTypeMap[e.FieldType].GetInstanceTypeEnsureDef()).ToArray());
            return m_StaticStorageType;
        }

        protected override CodeGenValue SetupVirtualTable() {
            throw new NotImplementedException();
        }
    }
}
