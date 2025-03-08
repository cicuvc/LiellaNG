using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Utils;
using System.Diagnostics;
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

        public override ITypeEntry ExactEntry => Entry;

        public LcTypeDefInfo(TypeDefEntry entry, LcCompileContext typeContext, CodeGenContext cgContext) : base(entry, typeContext) {
            var layoutAttribute = Entry.CustomAttributes
                .Where(e => e.ctor.DeclType.FullName == ".System.Runtime.InteropServices.StructLayoutAttribute")
                .Select(e => (LayoutKind)e.arguments.FixedArguments[0].Value!)
                .FirstOrDefault(Entry.IsValueType ? LayoutKind.Sequential : LayoutKind.Auto);

            if(Entry.IsExplicitLayout) {
                Layout = LayoutKind.Explicit;
            } else {
                Layout = layoutAttribute;
            }

            CgContext = cgContext;
        }
        public override LcTypeInfo ResolveContextType(ITypeEntry entry) {
            if(!Context.NativeTypeMap.ContainsKey(entry)) Debugger.Break();
            return Context.NativeTypeMap.GetValueOrDefault(entry,null!);
        }
        public override LcMethodInfo ResolveContextMethod(IMethodEntry entry) {
            return Context.NativeMethodMap.GetValueOrDefault(entry, null!);
        }
        protected override ICGenType SetupReferenceType() {
            return CgContext.TypeFactory.CreatePointer(GetDataStorageTypeEnsureDef()!);
        }
        protected override ICGenNamedStructType SetupDataStorage() {
            var dataStorageElements = new List<ICGenType>();
            var dataStorageType = CgContext.TypeFactory.CreateStruct($"data.{ExactEntry.FullName}");

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
                dataStorageType.SetStructBody((baseStorage is null || Entry.IsValueType) ? types.ToArray() : types.Prepend(baseStorage).ToArray());
            }
            return dataStorageType;
        }
        protected override ICGenType SetupInstanceType() {
            if(Entry is TypeDefEntry typeDef) {
                if(typeDef.GetDetails().IsEnum) {
                    var fieldType = typeDef.GetField("value__").FieldType;
                    return Context.NativeTypeMap[fieldType].GetInstanceTypeEnsureDef();
                }
            }
            if(Entry.IsInterface) {
                return CgContext.TypeFactory.CreateStruct([
                    CgContext.TypeFactory.VoidPtr, 
                    CgContext.TypeFactory.CreatePointer(GetDataStorageTypeEnsureDef())
                    ]);
            }
            return Entry.IsValueType ? 
                GetDataStorageTypeEnsureDef() : 
                CgContext.TypeFactory.CreatePointer(GetDataStorageTypeEnsureDef()!);
        }
        protected override ICGenNamedStructType SetupStaticStorage() {
            var staticStorageType = CgContext.TypeFactory.CreateStruct($"static.{ExactEntry.FullName}");
            staticStorageType.SetStructBody(
               Entry.TypeFields
               .Where(e => e.Attributes.HasFlag(FieldAttributes.Static))
               .Select(e => Context.NativeTypeMap[e.FieldType].GetInstanceTypeEnsureDef()).ToArray());
            return staticStorageType;
        }

        protected override ICGenNamedStructType SetupVirtualTableType() {
            var typeFactory = CgContext.TypeFactory;
            var intefaceLutType = Context.InterfaceLutType;
            var constGenerator = CgContext.ConstGenerator;
            var vtableType = CgContext.TypeFactory.CreateStruct($"vtable.{ExactEntry.FullName}");

            var primaryVTableTerms = GetVTableLayoutEnsureDef()
                .Select(e => {
                    if(e.methodDef is ITypeEntry typeEntry) {
                        if(typeEntry == ExactEntry) return typeFactory.CreatePointer(vtableType);
                        return typeFactory.CreatePointer(ResolveContextType(typeEntry).GetVirtualTableType());
                    } else if(e.methodDef is IMethodEntry methodEntry) {
                        return typeFactory.CreatePointer(ResolveContextMethod(methodEntry).GetMethodTypeEnsureDef());
                    }
                    throw new NotImplementedException();
                }).ToArray();


            var primaryTableType = typeFactory.CreateStruct(primaryVTableTerms.ToArray());

            var interfaceVTables = Entry.ImplInterfaces
                .Select(e => ResolveContextType(e.Key).GetVTablePtr().ElementType).ToArray();

            vtableType.SetStructBody([
                typeFactory.Int32, typeFactory.Int32,
                primaryTableType,
                typeFactory.CreateArray(Context.InterfaceLutType, interfaceVTables.Length),
                ..interfaceVTables
                ]);

            return vtableType;
        }

        /* VTable structure:
         * [   PTS     ][    ITS    ]
         * [      Primary table     ]
         * [   IID     ][   Offset  ]
         * [   IID     ][   Offset  ]
         * [    Interface tables    ]
         */
        /// <summary>
        /// Setup complete virtual table of type
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override CodeGenGlobalPtrValue SetupVirtualTable() {
            var typeFactory = CgContext.TypeFactory;
            var intefaceLutType = Context.InterfaceLutType;
            var constGenerator = CgContext.ConstGenerator;
            var vTableType = GetVirtualTableType();

            if(Entry.IsInterface) {
                var vtablePtr = CgContext.CreateGlobalValue($"ivt.{ExactEntry.FullName}", vTableType, null);
                vtablePtr.Initializer = constGenerator.CreateConstStruct(vTableType, [vtablePtr]);
                return vtablePtr;
            }

            var primaryTableLayout = GetVTableLayoutEnsureDef();
            var primaryTableType = (ICGenStructType)vTableType.Fields[2].type;


            var interfaceVTables = Entry.ImplInterfaces
                .Select(e => ResolveContextType(e.Key).GetVTablePtr().ElementType).ToArray();

            // =====================================

            var vtableGlobalPtr = CgContext.CreateGlobalValue($"vtx.{Entry.FullName}", vTableType, null);

            var primaryVTableSlots = new CodeGenValue[primaryTableLayout.Count];

            if(Entry.BaseType is not null) {
                var baseTypeInfo = ResolveContextType(Entry.BaseType);
                var baseVTable = baseTypeInfo.GetVTablePtr();
                var basePrimaryTable = (CodeGenConstStructValue)((CodeGenConstStructValue)baseVTable.Initializer!).Values[2];
                basePrimaryTable.Values.CopyTo(((Span<CodeGenValue>)primaryVTableSlots).Slice(0, basePrimaryTable.Values.Length));
            }

            foreach(var i in m_Methods) {
                if(i.IsVirtualDef || i.IsVirtualOverride) {
                    var index = LookupVtableSlotIndex(i.Entry);
                    primaryVTableSlots[index] = i.GetMethodValueEnsureDef();
                }
            }
            primaryVTableSlots[primaryVTableSlots.Length - 1] = vtableGlobalPtr;

            var iidTableSize = Entry.ImplInterfaces.Count * intefaceLutType.Size;
            var intefaceVtableOffsets = interfaceVTables.Select(e => e.Size).ExclusiveCumulativeSum().Select(e=>e+ iidTableSize).ToArray();

            var iidTable = Entry.ImplInterfaces.Keys.Select((e,idx) => {
                var lcType = ResolveContextType(e);
                var iidTermOffset = intefaceLutType.Size * (idx + 1);
                var offset = intefaceVtableOffsets[idx] - iidTermOffset;
                return constGenerator.CreateConstStruct(intefaceLutType, [lcType.InterfaceID, offset]);
            }).ToArray();
            var iidTableType = typeFactory.CreateArray(intefaceLutType, iidTable.Length);

            var interfaceVTable = Entry.ImplInterfaces.Select((e, idx) => {
                var lcType = ResolveContextType(e.Key);
                var vtableLayout = lcType.GetVTableLayoutEnsureDef();
                var iVTable = new CodeGenValue[vtableLayout.Count];

                foreach(var (prototype, impl) in e.Value) {
                    var implMethodInfo = ResolveContextMethod(impl);
                    var slotIndex = vtableLayout.FindIndex(e => e.methodDef == prototype);
                    iVTable[slotIndex] = implMethodInfo.GetMethodValueEnsureDef();
                }
                iVTable[iVTable.Length - 1] = lcType.GetVTablePtr();

                return constGenerator.CreateConstStruct(lcType.GetVirtualTableType(), iVTable);
            }).ToArray();


            var vtableInitValue = constGenerator.CreateConstStruct(vTableType, [
                primaryTableLayout.Count, interfaceVTables.Length,
                constGenerator.CreateConstStruct(primaryTableType, primaryVTableSlots),
                constGenerator.CreateConstArray(iidTableType, iidTable),
                ..interfaceVTable
            ]);

            vtableGlobalPtr.Initializer = vtableInitValue;


            return vtableGlobalPtr;
        }

        protected virtual int LookupVtableSlotIndex(IMethodEntry entry) {
            var vtableLayout = GetVTableLayoutEnsureDef();
            var virtualPrototype = entry.VirtualMethodPrototype;

            var layoutSize = vtableLayout.Count;
            for(var i = 0; i < layoutSize; i++) {
                var (methodDef, offset) = vtableLayout[i];

                var vtableMethodDef = methodDef is MethodInstantiation inst ? inst.Definition : methodDef;

                if(vtableMethodDef != virtualPrototype) continue;

                if((entry is not MethodInstantiation) && (methodDef is not MethodInstantiation))
                    return i;

                if((entry is MethodInstantiation entryInst) && (methodDef is MethodInstantiation vInst)) {
                    if(entryInst.ActualArguments.SequenceEqual(vInst.ActualArguments)) {
                        return i;
                    }
                }
            }

            throw new KeyNotFoundException();
        }

        protected override List<(IEntityEntry, int)> SetupVTableLayout() {
            var vtableLayout = new List<(IEntityEntry, int)>();
            var vtStartIdx = 0;
            var pointerSize = CgContext.TypeManager.Configuration.PointerSize;


            if(Entry.BaseType is not null) {
                var baseTypeInfo = ResolveContextType(Entry.BaseType);
                var baseVTableLayout = baseTypeInfo.GetVTableLayoutEnsureDef();

                vtStartIdx = baseVTableLayout.Last().index + pointerSize;

                vtableLayout.AddRange(baseVTableLayout);
            }

            foreach(var i in m_Methods) {
                if(i.IsVirtualDef) { // New slot
                    vtableLayout.Add((i.Entry, vtStartIdx));
                    vtStartIdx += pointerSize;
                }
            }

            vtableLayout.Add((ExactEntry, vtStartIdx));

            return vtableLayout;
        }
    }
}
