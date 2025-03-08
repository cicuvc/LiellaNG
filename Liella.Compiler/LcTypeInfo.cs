using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata;
using Liella.TypeAnalysis.Metadata.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * A CLR type is mapped into 5 native types
 * Static storage (S) => a struct containing all unique static fields
 * Data Storage Type (D) => a struct containing all instance fields
 * 
 *                                                     v
 * Reference Type (R) => type for object on heap ([V*, D]) point to first byte of data storage D, only use for type of pthis
 * 
 * Instance Type (I) => (R) for non-primitive type, (backend builtin-types) for primitive types
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
        StaticStorageComplete = 0x40,

        VTablePending = 0x80,
        VTableComplete = 0x100,
        VTableLayoutPending = 0x200,
        VTableLayoutComplete = 0x400,
        VTableTypePending = 0x800,
        VTableTypeComplete = 0x1000,

        ReferenceTypePending = 0x2000,
        ReferenceTypeComplete = 0x4000
    }
    public abstract class LcTypeInfo {
        private ICGenType? m_InstanceType;
        private ICGenType? m_ReferenceType;
        private ICGenNamedStructType? m_DataStorageType;
        private ICGenNamedStructType? m_StaticStorageType;
        private ICGenNamedStructType? m_VTableType;
        private CodeGenGlobalPtrValue? m_VTableStoragePtr;
        protected List<LcMethodInfo> m_Methods = new();
        protected List<(IEntityEntry methodDef, int index)>? m_VTableLayout;
        // Always type def entry
        public ITypeEntry Entry { get; }
        public abstract ITypeEntry ExactEntry { get; } 
        public LcTypeInitStage InitState { get; protected set; }
        public LcCompileContext Context { get; }
        public abstract bool IsStorageRequired { get; }
        public IReadOnlyList<LcMethodInfo> Methods => m_Methods;
        public int InterfaceID { get; }

        public abstract LcTypeInfo? ResolveContextType(ITypeEntry entry);
        public abstract LcMethodInfo? ResolveContextMethod(IMethodEntry entry);
        public ICGenType GetInstanceTypeEnsureDef() {
            if(!CheckTypeInitialized(LcTypeInitStage.InstancePending, LcTypeInitStage.InstanceComplete)) {
                m_InstanceType = SetupInstanceType();
                SetTypeInitialized(LcTypeInitStage.InstancePending, LcTypeInitStage.InstanceComplete);
            }
            return m_InstanceType!;
        }
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
        public ICGenNamedStructType GetDataStorageTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.DataStoragePending, LcTypeInitStage.DataStorageComplete)) {
                m_DataStorageType = SetupDataStorage();
                SetTypeInitialized(LcTypeInitStage.DataStoragePending, LcTypeInitStage.DataStorageComplete);
            }
            return m_DataStorageType!;
        }
        public ICGenType GetReferenceTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.ReferenceTypePending, LcTypeInitStage.ReferenceTypeComplete)) {
                m_ReferenceType = SetupReferenceType();
                SetTypeInitialized(LcTypeInitStage.ReferenceTypePending, LcTypeInitStage.ReferenceTypeComplete);
            }
            return m_ReferenceType!;
        }
        public ICGenNamedStructType GetStaticStorageTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete)) {
                m_StaticStorageType = SetupStaticStorage();
                SetTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete);
            }
            return m_StaticStorageType!;
        }
        public ICGenNamedStructType GetVirtualTableType() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.VTableTypeComplete, LcTypeInitStage.VTableTypePending)) {
                m_VTableType = SetupVirtualTableType();
                SetTypeInitialized(LcTypeInitStage.VTableTypeComplete, LcTypeInitStage.VTableTypePending);
            }
            return m_VTableType!;
        }
        public CodeGenGlobalPtrValue GetVTablePtr() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.VTablePending, LcTypeInitStage.VTableComplete)) {
                m_VTableStoragePtr = SetupVirtualTable();
                SetTypeInitialized(LcTypeInitStage.VTablePending, LcTypeInitStage.VTableComplete);
            }
            return m_VTableStoragePtr!;
        }
        public List<(IEntityEntry methodDef, int index)> GetVTableLayoutEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.VTableLayoutPending, LcTypeInitStage.VTableLayoutComplete)) {
                m_VTableLayout = SetupVTableLayout();
                SetTypeInitialized(LcTypeInitStage.VTableLayoutPending, LcTypeInitStage.VTableLayoutComplete);
            }

            return m_VTableLayout!;
        }
        protected abstract ICGenType SetupInstanceType();
        protected abstract ICGenNamedStructType SetupDataStorage();
        protected abstract ICGenType SetupReferenceType();
        protected abstract ICGenNamedStructType SetupStaticStorage();
        protected abstract ICGenNamedStructType SetupVirtualTableType();
        protected abstract CodeGenGlobalPtrValue SetupVirtualTable();
        protected abstract List<(IEntityEntry, int)> SetupVTableLayout();
        
        protected LcTypeInfo(ITypeEntry entry, LcCompileContext context) {
            Entry = entry;
            Context = context;

            if(entry.IsInterface) {
                InterfaceID = Context.RegisterInterface(this);
            }
        }

        public void RegisterMethod(LcMethodInfo method) {
            m_Methods.Add(method);
        }


        public override string ToString() {
            return $"{GetType().Name}: {ExactEntry}";
        }
    }
}
