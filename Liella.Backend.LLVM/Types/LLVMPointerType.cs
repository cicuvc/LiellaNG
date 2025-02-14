using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMPointerType : CGenAbstractType<LLVMPointerType, LLVMPointerTag>, ICGenType<LLVMPointerType>, ICGenPointerType, ILLVMType
    {
        public override CGenTypeTag Tag => CGenTypeTag.Pointer;

        public ICGenType ElementType => InvariantPart.ELementType;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;
        public LLVMPointerType(in LLVMPointerTag tag) : base(tag) { }
        public static LLVMPointerType CreateFromKey(LLVMPointerType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMPointerType Create(ICGenType elementType, int asid, CodeGenTypeManager manager)
        {
            var elementTypeRef = ((ILLVMType)elementType).InternalType;
            return CreateEntry(manager, new LLVMPointerTag(LLVMTypeRef.CreatePointer(elementTypeRef, (uint)asid), elementType));
        }
    }
}
