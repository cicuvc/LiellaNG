using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct PrimitiveTypeEntryDetails : IDetails<PrimitiveTypeEntry>
    {
        public TypeDefEntry DefinitionType { get; private set; }
        public bool IsValid => DefinitionType is not null;
        public void CreateDetails(PrimitiveTypeEntry entry)
        {
            var typeNode = entry.TypeEnv.TokenResolver.ResolvePrimitiveType(entry.InvariantPart.TypeCode);
            DefinitionType = TypeDefEntry.Create(entry.TypeEnv.EntryManager, typeNode.AsmInfo, typeNode.TypeDef);
        }
    }
}
