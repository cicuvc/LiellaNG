using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct GenericPlaceholderDetails : IDetails<GenericPlaceholderTypeEntry>
    {
        public GenericPlaceholderTypeEntry Entry { get; private set; }
        public GenericParameter ParamDef { get; private set; }
        public string Name { get; private set; }
        public bool IsValid => Entry is not null;
        public void CreateDetails(GenericPlaceholderTypeEntry entry)
        {
            var metaReader = entry.AsmInfo.MetaReader;

            Entry = entry;
            ParamDef = metaReader.GetGenericParameter(entry.InvariantPart.ParamDef);
            Name = metaReader.GetString(ParamDef.Name);
        }
    }
}
