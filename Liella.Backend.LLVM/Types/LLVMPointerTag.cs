using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMPointerTag : IEquatable<LLVMPointerTag>
    {
        public LLVMTypeRef InternalType { get; }
        public ICGenType ELementType { get; }
        public LLVMPointerTag(LLVMTypeRef internalType, ICGenType elementType)
        {
            InternalType = internalType;
            ELementType = elementType;
        }
        public override int GetHashCode()
        {
            return ELementType.GetHashCode() ^ 0x114514;
        }
        public bool Equals(LLVMPointerTag other)
        {
            return other.InternalType == InternalType && other.ELementType == ELementType;
        }
    }
}
