using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMStructTag : IEquatable<LLVMStructTag>
    {
        public LLVMTypeRef InternalType { get; }
        public string? Name { get; }
        public ImmutableArray<ICGenType> StructTypes { get; }
        public bool Packed { get; }
        public LLVMStructTag(LLVMTypeRef internalType, ImmutableArray<ICGenType> structTypes, bool packed, string? name = null)
        {
            InternalType = internalType;
            StructTypes = structTypes;
            Packed = packed;
            Name = name;
        }
        public override int GetHashCode()
        {
            return InternalType.Handle.GetHashCode();
        }
        public bool Equals(LLVMStructTag other)
        {
            return other.InternalType == InternalType;
        }
    }
}
