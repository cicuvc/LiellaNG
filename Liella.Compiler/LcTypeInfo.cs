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
        StaticStorageComplete = 0x40,

        VTablePending = 0x80,
        VTableComplete = 0x100
    }
    public abstract class LcTypeInfo {
        protected ICGenType? m_InstanceType;
        protected ICGenNamedStructType? m_DataStorageType;
        protected ICGenNamedStructType? m_StaticStorageType;
        protected ICGenNamedStructType? m_VTableType;
        protected CodeGenValue? m_VTableStoragePtr;
        protected List<LcMethodInfo> m_Methods = new();
        public ITypeEntry Entry { get; }
        public LcTypeInitStage InitState { get; protected set; }
        public LcCompileContext Context { get; }
        public abstract bool IsStorageRequired { get; }
        public IReadOnlyList<LcMethodInfo> Methods => m_Methods;

        public abstract LcTypeInfo? ResolveContextType(ITypeEntry entry);
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
        public ICGenNamedStructType GetStaticStorageTypeEnsureDef() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete)) {
                m_StaticStorageType = SetupStaticStorage();
                SetTypeInitialized(LcTypeInitStage.StaticStoragePending, LcTypeInitStage.StaticStorageComplete);
            }
            return m_StaticStorageType!;
        }
        public CodeGenValue GetVTablePtr() {
            if(!IsStorageRequired) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcTypeInitStage.VTablePending, LcTypeInitStage.VTableComplete)) {
                m_VTableStoragePtr = SetupVirtualTable();
                SetTypeInitialized(LcTypeInitStage.VTablePending, LcTypeInitStage.VTableComplete);
            }
            return m_VTableStoragePtr!;
        }
        protected abstract ICGenType SetupInstanceType();
        protected abstract ICGenNamedStructType SetupDataStorage();
        protected abstract ICGenNamedStructType SetupStaticStorage();
        protected abstract CodeGenValue SetupVirtualTable();
        
        protected LcTypeInfo(ITypeEntry entry, LcCompileContext context) {
            Entry = entry;
            Context = context;
        }

        public void RegisterMethod(LcMethodInfo method) {
            m_Methods.Add(method);
        }
    }
}
