using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMVoidType : CGenAbstractType<LLVMVoidType, LLVMVoidTag>, ICGenType<LLVMVoidType>, ILLVMType
    {
        public LLVMVoidType(in LLVMVoidTag invariant) : base(invariant)
        {
        }

        public override CGenTypeTag Tag => CGenTypeTag.Void;

        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public static LLVMVoidType CreateFromKey(LLVMVoidType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMVoidType Create(CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new());
        }
    }
}
