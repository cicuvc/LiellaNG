using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMArrayTag : IEquatable<LLVMArrayTag>
    {
        public LLVMTypeRef InternalType { get; }
        public ICGenType ELementType { get; }
        public int Length { get; }
        public LLVMArrayTag(LLVMTypeRef internalType, ICGenType elementType, int length)
        {
            InternalType = internalType;
            ELementType = elementType;
            Length = length;
        }
        public override int GetHashCode()
        {
            return InternalType.Handle.GetHashCode();
        }
        public bool Equals(LLVMArrayTag other)
        {
            return other.InternalType == InternalType;
        }
    }
}
