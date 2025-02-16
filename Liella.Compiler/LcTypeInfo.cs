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
using System.Reflection.Metadata;
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
    public enum LcTypeInitStage {
        DeclareComplete = 0x1, 
        InstancePending = 0x2,
        InstanceComplete = 0x4,
        DataStoragePending = 0x8,
        DataStorageComplete = 0x10,
        StaticStoragePending = 0x20,
        StaticStorageComplete = 0x40
    }
    public abstract class LcTypeInfo {
        protected ICGenType? m_InstanceType;
        protected ICGenNamedStructType? m_DataStorageType;
        protected ICGenNamedStructType? m_StaticStorageType;
        public ITypeEntry Entry { get; }
        public LcTypeInitStage InitState { get; protected set; }
        public LcCompileContext Context { get; }
        public abstract bool IsStorageRequired { get; }
        public abstract ICGenType? VirtualTableType { get; }
        public ICGenType GetInstanceTypeEnsureDef() {
            if(!CheckTypeInitialized(LcTypeInitStage.InstancePending, LcTypeInitStage.InstanceComplete)) {
                m_InstanceType = SetupInstanceType();
                SetTypeInitialized(LcTypeInitStage.InstancePending, LcTypeInitStage.InstanceComplete);
            }
            return m_InstanceType!;
        }
        public ICGenNamedStructType GetDataStorageTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.DataStoragePending, LcTypeInitStage.DataStorageComplete)) {
                m_DataStorageType = SetupDataStorage();
                SetTypeInitialized(LcTypeInitStage.DataStoragePending, LcTypeInitStage.DataStorageComplete);
            }
            return m_DataStorageType!;
        }
        public ICGenNamedStructType GetStaticStorageTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete)) {
                m_StaticStorageType = SetupStaticStorage();
                SetTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete);
            }
            return m_StaticStorageType!;
        }
        protected abstract ICGenType SetupInstanceType();
        protected abstract ICGenNamedStructType SetupDataStorage();
        protected abstract ICGenNamedStructType SetupStaticStorage();
        protected bool CheckTypeInitialized(LcTypeInitStage pending, LcTypeInitStage complete) {
            if(InitState.HasFlag(complete)) return true;
            if(InitState.HasFlag(pending)) {
                throw new InvalidOperationException("Bad type structure cause infinity recursive");
            }
            InitState ^= pending;
            return false;
        }
        protected void SetTypeInitialized(LcTypeInitStage pending, LcTypeInitStage complete) {
            InitState ^= pending ^ complete;
        }
        protected LcTypeInfo(ITypeEntry entry, LcCompileContext context) {
            Entry = entry;
            Context = context;
        }
    }
    public class LcPrimitiveTypeInfo: LcTypeDefInfo {
        public PrimitiveTypeEntry PrimitiveType { get; }
        public LcPrimitiveTypeInfo(TypeDefEntry implType, PrimitiveTypeEntry primitiveType, LcCompileContext typeContext, CodeGenContext cgContext) :base(implType, typeContext, cgContext) {
            PrimitiveType = primitiveType;
        }
        protected override ICGenType SetupInstanceType() {
            return PrimitiveType.InvariantPart.TypeCode switch {
                PrimitiveTypeCode.Boolean => CgContext.TypeFactory.Int1,
                PrimitiveTypeCode.SByte => CgContext.TypeFactory.CreateIntType(8, false),
                PrimitiveTypeCode.Int16 => CgContext.TypeFactory.CreateIntType(16, false),
                PrimitiveTypeCode.Int32 => CgContext.TypeFactory.CreateIntType(32, false),
                PrimitiveTypeCode.Int64 => CgContext.TypeFactory.CreateIntType(64, false),
                PrimitiveTypeCode.Byte => CgContext.TypeFactory.CreateIntType(8, true),
                PrimitiveTypeCode.UInt16 => CgContext.TypeFactory.CreateIntType(16, true),
                PrimitiveTypeCode.UInt32 => CgContext.TypeFactory.CreateIntType(32, true),
                PrimitiveTypeCode.UInt64 => CgContext.TypeFactory.CreateIntType(64, true),
                PrimitiveTypeCode.Void => CgContext.TypeFactory.Void,
                PrimitiveTypeCode.Double => CgContext.TypeFactory.Float64,
                PrimitiveTypeCode.Single => CgContext.TypeFactory.Float32,
                _ => throw new NotSupportedException()
            };
        }
    }
    public class LcPointerTypeInfo : LcTypeInfo {
        public override bool IsStorageRequired => false;
        public bool IsTypeDefined { get; protected set; }
        public bool IsPointer { get; }
        public CodeGenContext CgContext { get; }
        public ITypeEntry ElementEntry { get; }
        public override ICGenType? VirtualTableType => throw new NotImplementedException();
        public LcPointerTypeInfo(ITypeEntry entry, LcCompileContext typeContext, CodeGenContext cgContext) :base(entry, typeContext) {
            CgContext = cgContext;

            if(entry is PointerTypeEntry pointer) {
                ElementEntry = pointer.InvariantPart.BaseType;
                IsPointer = true;
            } else if(entry is ReferenceTypeEntry reference) {
                ElementEntry = reference.InvariantPart.BaseType;
                IsPointer = false;
            } else throw new NotSupportedException();
        }

        protected override ICGenType SetupInstanceType() {
            return CgContext.TypeFactory.CreatePointer(Context.NativeTypeMap[ElementEntry].GetInstanceTypeEnsureDef());
        }

        protected override ICGenNamedStructType SetupDataStorage() 
            => throw new NotSupportedException();
        protected override ICGenNamedStructType SetupStaticStorage()
            => throw new NotSupportedException();
    }
    public class LcTypeInstInfo : LcTypeDefInfo {
        public TypeInstantiationEntry TypeInstantiation { get; }
        public LcTypeInstInfo(TypeInstantiationEntry instEntry,TypeDefEntry defEntry, LcCompileContext typeContext, CodeGenContext cgContext) : base(defEntry, typeContext, cgContext) {
            TypeInstantiation = instEntry;
        }
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
    public class LcTypeDefInfo : LcTypeInfo {
        protected Dictionary<FieldDefEntry, (int offset, int index)> m_DataStorageLayout = new();
        protected Dictionary<MethodDefEntry, LcMethodInfo> m_Methods = new();
        public override bool IsStorageRequired => true;
        public ICGenType? InstanceType { get; protected set; }
        public override ICGenType? VirtualTableType => throw new NotImplementedException();
        public IReadOnlyDictionary<FieldDefEntry, (int offset, int index)> DataStorageLayout => m_DataStorageLayout;
        public IReadOnlyDictionary<MethodDefEntry, LcMethodInfo> Methods => m_Methods;
        public LayoutKind Layout { get; }
        public CodeGenContext CgContext { get; }
        
        public LcTypeDefInfo(TypeDefEntry entry, LcCompileContext typeContext, CodeGenContext cgContext) : base(entry, typeContext) {

            m_StaticStorageType = cgContext.TypeFactory.CreateStruct($"static.{entry.FullName}");
            m_DataStorageType = cgContext.TypeFactory.CreateStruct($"data.{entry.FullName}");

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
        protected virtual LcTypeInfo ResolveContextType(ITypeEntry entry) {
            return Context.NativeTypeMap[entry];
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
            return Entry.IsValueType ? m_DataStorageType! : CgContext.TypeFactory.CreatePointer(m_DataStorageType!);
        }
        protected override ICGenNamedStructType SetupStaticStorage() {
            m_StaticStorageType!.SetStructBody(
               Entry.TypeFields
               .Where(e => e.Attributes.HasFlag(FieldAttributes.Static))
               .Select(e => Context.NativeTypeMap[e.FieldType].GetInstanceTypeEnsureDef()).ToArray());
            return m_StaticStorageType;
        }
    }
}
