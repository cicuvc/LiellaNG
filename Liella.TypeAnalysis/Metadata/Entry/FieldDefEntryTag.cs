using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct FieldDefEntryTag : IEquatable<FieldDefEntryTag>
    {
        public AssemblyReaderTuple AsmInfo { get; }
        public FieldDefinitionHandle FieldDef { get; }
        public FieldDefEntryTag(AssemblyReaderTuple asmInfo, FieldDefinitionHandle fieldDef)
        {
            AsmInfo = asmInfo;
            FieldDef = fieldDef;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(AsmInfo);
            hashCode.Add(FieldDef);
            return hashCode.ToHashCode() << 4 | (int)EntryTagType.FieldEntry;
        }

        public bool Equals(FieldDefEntryTag other)
        {
            return other.AsmInfo.Equals(AsmInfo) && other.FieldDef.Equals(FieldDef);
        }
    }
}
