using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMNumericType : CGenAbstractType<LLVMNumericType, LLVMNumericTypeTag>, ICGenType<LLVMNumericType>, ICGenNumericType, ILLVMType
    {
        public override CGenTypeTag Tag
        {
            get
            {
                var type = InvariantPart.IsFloat ? CGenTypeTag.Float : CGenTypeTag.Integer;
                return (InvariantPart.IsUnsigned ? CGenTypeTag.Unsigned : 0) | CGenTypeTag.Integer;
            }
        }

        public int Width => (int)InvariantPart.InternalType.IntWidth;

        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public LLVMNumericType(LLVMNumericTypeTag tag) : base(tag)
        {
        }
        public static LLVMNumericType CreateFromKey(LLVMNumericType key, CodeGenTypeManager manager)
        {
            return new LLVMNumericType(key.InvariantPart);
        }
        public static LLVMNumericType CreateInt(int width, CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new(LLVMTypeRef.CreateInt((uint)width), false, false));
        }
        public static LLVMNumericType CreateUInt(int width, CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new(LLVMTypeRef.CreateInt((uint)width), false, true));
        }
        public static LLVMNumericType CreateFloat32(CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new(LLVMTypeRef.Float, true, false));
        }
        public static LLVMNumericType CreateFloat64(CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new(LLVMTypeRef.Double, true, false));
        }
    }
}
