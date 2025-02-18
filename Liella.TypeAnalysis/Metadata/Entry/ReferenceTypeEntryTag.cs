using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public struct ReferenceTypeEntryTag : IEquatable<ReferenceTypeEntryTag> {
        public ITypeEntry ElementType { get; }
        public ReferenceTypeEntryTag(ITypeEntry baseType) {
            ElementType = baseType;
        }
        public override int GetHashCode() {
            return ElementType.GetHashCode() << 4 | (int)EntryTagType.Reference;
        }

        public bool Equals(ReferenceTypeEntryTag other) => other.ElementType.Equals(ElementType);
    }
}
