using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct TypeInstantiationEntryTag : IEquatable<TypeInstantiationEntryTag>
    {
        public TypeDefEntry DefinitionType { get; }
        public ImmutableArray<ITypeEntry> TypeArguments { get; }
        public bool IsPrimary { get; }
        public TypeInstantiationEntryTag(TypeDefEntry entry, ImmutableArray<ITypeEntry> typeArguments, bool isPrimary)
        {
            DefinitionType = entry;
            TypeArguments = typeArguments;
            IsPrimary = isPrimary;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            //hashCode.Add(IsPrimary);
            hashCode.Add(DefinitionType);
            foreach (var i in TypeArguments)
                hashCode.Add(i);
            return hashCode.ToHashCode() << 4 | (int)EntryTagType.TypeInstEntry;
        }
        public bool Equals(TypeInstantiationEntryTag other)
        {
            return other.DefinitionType.Equals(DefinitionType) && other.TypeArguments.SequenceEqual(TypeArguments) && (IsPrimary == other.IsPrimary);
        }
    }
}
