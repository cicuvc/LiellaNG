using Liella.TypeAnalysis.Metadata.Elements;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct EmptyDetails<TEntry> : IDetails<TEntry> where TEntry : IEntityEntry<TEntry>
    {
        public bool IsValid => true;

        public void CreateDetails(TEntry entry) { }
    }
}
