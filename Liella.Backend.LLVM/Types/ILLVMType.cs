using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public interface ILLVMType
    {
        internal LLVMTypeRef InternalType { get; }
    }
}
