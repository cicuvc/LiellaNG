using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public struct PointerTypeEntryTag : IEquatable<PointerTypeEntryTag>
    {
        public ITypeEntry ElementType { get; }
        public PointerTypeEntryTag(ITypeEntry baseType)
        {
            ElementType = baseType;
        }
        public override int GetHashCode()
        {
            return ElementType.GetHashCode() << 4 | (int)EntryTagType.Pointer;
        }

        public bool Equals(PointerTypeEntryTag other) => other.ElementType.Equals(ElementType);
    }
}
