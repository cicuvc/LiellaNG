using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public struct ReferenceTypeEntryTag : IEquatable<ReferenceTypeEntryTag> {
        public ITypeEntry BaseType { get; }
        public ReferenceTypeEntryTag(ITypeEntry baseType) {
            BaseType = baseType;
        }
        public override int GetHashCode() {
            return BaseType.GetHashCode() << 4 | (int)EntryTagType.Reference;
        }

        public bool Equals(ReferenceTypeEntryTag other) => other.BaseType.Equals(BaseType);
    }
}
