using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler;
using Liella.TypeAnalysis.Metadata.Entry;
using System.Reflection.Metadata;

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
    public class LcPrimitiveTypeInfo: LcTypeDefInfo {
        public PrimitiveTypeEntry PrimitiveType { get; }
        public PrimitiveTypeCode PrimitiveCode { get; }
        public LcPrimitiveTypeInfo(TypeDefEntry implType, PrimitiveTypeEntry primitiveType, LcCompileContext typeContext, CodeGenContext cgContext) :base(implType, typeContext, cgContext) {
            PrimitiveType = primitiveType;
            PrimitiveCode = primitiveType.InvariantPart.TypeCode;
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
                PrimitiveTypeCode.String => CgContext.TypeFactory.CreatePointer(GetDataStorageTypeEnsureDef()!),
                PrimitiveTypeCode.Object => CgContext.TypeFactory.CreatePointer(GetDataStorageTypeEnsureDef()!),
                PrimitiveTypeCode.Char => CgContext.TypeFactory.CreateIntType(16, false),
                PrimitiveTypeCode.IntPtr => CgContext.TypeFactory.CreateIntType(8 * CgContext.TypeManager.Configuration.PointerSize, false),
                PrimitiveTypeCode.UIntPtr => CgContext.TypeFactory.CreateIntType(8 * CgContext.TypeManager.Configuration.PointerSize, true),
                _ => throw new NotSupportedException()
            };
        }
    }
}
