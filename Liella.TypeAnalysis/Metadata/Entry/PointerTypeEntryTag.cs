using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public struct PointerTypeEntryTag : IEquatable<PointerTypeEntryTag>
    {
        public ITypeEntry BaseType { get; }
        public PointerTypeEntryTag(ITypeEntry baseType)
        {
            BaseType = baseType;
        }
        public override int GetHashCode()
        {
            return BaseType.GetHashCode() << 4 | (int)EntryTagType.Pointer;
        }

        public bool Equals(PointerTypeEntryTag other) => other.BaseType.Equals(BaseType);
    }
}
