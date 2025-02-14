using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct TypeDefEntryTag : IEquatable<TypeDefEntryTag>
    {
        public AssemblyReaderTuple AsmInfo { get; }
        public TypeDefinitionHandle TypeDef { get; }
        public TypeDefEntryTag(AssemblyReaderTuple asmInfo, TypeDefinitionHandle typeHandle)
        {
            AsmInfo = asmInfo;
            TypeDef = typeHandle;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(AsmInfo);
            hashCode.Add(TypeDef);
            return hashCode.ToHashCode() << 4 | (int)EntryTagType.TypeDefEntry;
        }

        public bool Equals(TypeDefEntryTag other)
        {
            return other.AsmInfo == AsmInfo && other.TypeDef == TypeDef;
        }
    }
}
