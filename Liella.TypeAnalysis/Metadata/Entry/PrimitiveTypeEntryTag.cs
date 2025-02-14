using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct PrimitiveTypeEntryTag : IEquatable<PrimitiveTypeEntryTag>
    {
        public PrimitiveTypeCode TypeCode { get; }
        public PrimitiveTypeEntryTag(PrimitiveTypeCode typeCode) => TypeCode = typeCode;
        public bool Equals(PrimitiveTypeEntryTag other) => other.TypeCode == TypeCode;
        public override int GetHashCode() => (int)TypeCode << 4 | (int)EntryTagType.Primitive;
    }
}
