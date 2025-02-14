using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct FieldDetails : IDetails<FieldDefEntry>
    {
        public FieldDefEntry Entry { get; private set; }
        public FieldDefinition FieldDef { get; private set; }
        public ITypeEntry FieldType { get; private set; }
        public ITypeEntry DeclType { get; private set; }
        public string Name => Entry.AsmInfo.MetaReader.GetString(FieldDef.Name);
        public bool IsValid => Entry is not null;
        public void CreateDetails(FieldDefEntry entry)
        {
            var metaReader = entry.AsmInfo.MetaReader;
            var typeEnv = entry.TypeEnv;


            Entry = entry;
            FieldDef = metaReader.GetFieldDefinition(entry.InvariantPart.FieldDef);

            DeclType = TypeDefEntry.Create(typeEnv.EntryManager, entry.AsmInfo, FieldDef.GetDeclaringType());

            FieldType = FieldDef.DecodeSignature(entry.TypeEnv.SignDecoder, new(DeclType.TypeArguments, DeclType.MethodArguments));
        }
    }
}
