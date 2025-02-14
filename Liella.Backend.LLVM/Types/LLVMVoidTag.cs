using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMVoidTag : IEquatable<LLVMVoidTag>
    {
        public LLVMTypeRef InternalType { get; }
        public LLVMVoidTag()
        {
            InternalType = LLVMTypeRef.Void;
        }
        public override int GetHashCode() => InternalType.Handle.GetHashCode();
        public bool Equals(LLVMVoidTag other)
        {
            return other.InternalType == InternalType;
        }
    }
}
