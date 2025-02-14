using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMNumericTypeTag : IEquatable<LLVMNumericTypeTag>
    {
        public LLVMTypeRef InternalType { get; }
        public bool IsFloat { get; }
        public bool IsUnsigned { get; }
        public LLVMNumericTypeTag(LLVMTypeRef typeRef, bool isFloat, bool isUnsigned)
        {
            InternalType = typeRef;
            IsFloat = isFloat;
            IsUnsigned = isUnsigned;
        }
        public override int GetHashCode()
        {
            return InternalType.Handle.GetHashCode();
        }
        public bool Equals(LLVMNumericTypeTag other)
        {
            return other.InternalType == InternalType && other.IsUnsigned == IsUnsigned;
        }
    }
}
