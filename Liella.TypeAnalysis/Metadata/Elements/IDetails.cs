namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IDetails<TEntry> where TEntry : IEntityEntry<TEntry>
    {
        bool IsValid { get; }
        void CreateDetails(TEntry entry);
    }
}
