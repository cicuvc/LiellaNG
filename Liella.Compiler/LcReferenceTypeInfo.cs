using Liella.Backend.Components;
using Liella.Backend.Types;
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
    //public class LcTemporaryReferenceTypeInfo : LcReferenceTypeInfo {
    //    protected static ReferenceTypeEntry GetReferenceType(LcCompileContext context, LcTypeInfo elementType) {
    //        if(elementType is LcReferenceTypeInfo refType) return (ReferenceTypeEntry)refType.Entry;
            
    //    }
    //    public LcTemporaryReferenceTypeInfo(LcTypeInfo elementType, LcCompileContext typeContext, CodeGenContext cgContext) : base(entry, typeContext, cgContext) {
    //    }
    //}
    public class LcReferenceTypeInfo : LcTypeInfo {
        public override bool IsStorageRequired => false;
        public bool IsTypeDefined { get; protected set; }
        public CodeGenContext CgContext { get; }
        public ITypeEntry ElementEntry { get; }

        public override ITypeEntry ExactEntry => throw new NotImplementedException();

        public LcReferenceTypeInfo(ReferenceTypeEntry entry, LcCompileContext typeContext, CodeGenContext cgContext) : base(entry, typeContext) {
            CgContext = cgContext;
            ElementEntry = entry.InvariantPart.ElementType;
        }

        protected override ICGenType SetupInstanceType() {
            return CgContext.TypeFactory.CreatePointer(Context.NativeTypeMap[ElementEntry].GetInstanceTypeEnsureDef());
        }

        protected override ICGenNamedStructType SetupDataStorage()
            => throw new NotSupportedException();
        protected override ICGenNamedStructType SetupStaticStorage()
            => throw new NotSupportedException();
        protected override CodeGenGlobalPtrValue SetupVirtualTable() {
            throw new NotSupportedException();
        }

        public override LcTypeInfo? ResolveContextType(ITypeEntry entry) {
            throw new NotImplementedException();
        }

        public override LcMethodInfo? ResolveContextMethod(IMethodEntry entry) {
            throw new NotImplementedException();
        }

        protected override List<(IEntityEntry, int)> SetupVTableLayout() {
            throw new NotImplementedException();
        }

        protected override ICGenNamedStructType SetupVirtualTableType() {
            throw new NotImplementedException();
        }

        protected override ICGenType SetupReferenceType() {
            throw new NotImplementedException();
        }
    }
}
