using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.TypeAnalysis.Metadata;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public abstract ICGenNamedStructType StaticStorage { get; }
        public abstract ICGenNamedStructType DataStorage { get; }
        public abstract ICGenType InstanceType { get; }
        public abstract ICGenType? VirtualTableType { get; }

        public abstract void SetupTypes();

        public LcTypeInfo(LcTypeContext context) {
            Context = context;
        }
    }
    
    public class LcTypeDefInfo : LcTypeInfo {
        protected Dictionary<FieldDefEntry, (int offset, int index)> m_DataStorageLayout = new();
        public ITypeEntry Entry { get; }
        public override ICGenNamedStructType StaticStorage { get; }
        public override ICGenNamedStructType DataStorage { get; }
        public override ICGenType InstanceType { get; }
        public override ICGenType? VirtualTableType => throw new NotImplementedException();
        public IReadOnlyDictionary<FieldDefEntry, (int offset, int index)> DataStorageLayout => m_DataStorageLayout;
        public bool IsExplicitLayout { get; }
        public LcTypeDefInfo(ITypeEntry entry, LcTypeContext typeContext, CodeGenContext cgContext) : base(typeContext) {
            Entry = entry;

            StaticStorage = cgContext.TypeFactory.CreateStruct($"static.{entry.FullName}");
            DataStorage = cgContext.TypeFactory.CreateStruct($"data.{entry.FullName}");

            InstanceType = entry.IsValueType ? DataStorage : cgContext.TypeFactory.CreatePointer(DataStorage);
            IsExplicitLayout = Entry.Attributes.HasFlag(TypeAttributes.ExplicitLayout);
        }
        public override void SetupTypes() {
            StaticStorage.SetStructBody(
                Entry.TypeFields
                .Where(e => e.Attributes.HasFlag(FieldAttributes.Static))
                .Select(e => Context.NativeTypeMap[e.FieldType].InstanceType).ToArray(), false);

            var dataStorageElements = new List<ICGenType>();
            // Must be struct
            if(IsExplicitLayout) {

            }
            if(Entry.BaseType is not null) {

            }
        }

    }
}
