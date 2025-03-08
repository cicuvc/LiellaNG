using Liella.Backend.Types;
using LLVMSharp;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMNamedStructTag : IEquatable<LLVMNamedStructTag> {
        public LLVMTypeRef InternalType { get; }
        public string Name { get; }
        public LLVMNamedStructTag(LLVMTypeRef internalType, string name) {
            InternalType = internalType;
            Name = name;
        }
        public override int GetHashCode() {
            return InternalType.Handle.GetHashCode() ^ (Name?.GetHashCode() ?? 0x114514);
        }
        public bool Equals(LLVMNamedStructTag other) {
            return other.InternalType == InternalType && other.Name == Name;
        }
    }
    public struct LLVMStructTag : IEquatable<LLVMStructTag>
    {
        public LLVMTypeRef InternalType { get; }
        public string? Name { get; }
        public ImmutableArray<(ICGenType type, int offset)> StructTypes { get; }
        public int Size { get; }
        public int Alignment { get; }
        public LLVMStructTag(LLVMTypeRef internalType, ImmutableArray<ICGenType> elements, string? name = null)
        {
            InternalType = internalType;
            StructTypes = elements.Zip(CGenStructLayoutHelpers.LayoutStruct(elements.AsSpan(), out var size)).ToImmutableArray();
            Name = name;

            Size = size;
            Alignment = elements.Select(e => e.Alignment).Max();
        }
        public override int GetHashCode()
        {
            return InternalType.Handle.GetHashCode() ^ (Name?.GetHashCode() ?? 0x114514) ^ (StructTypes.GetHashCode());
        }
        public bool Equals(LLVMStructTag other)
        {
            return other.InternalType == InternalType && other.Name == Name && StructTypes.SequenceEqual(other.StructTypes);
        }
    }
}
