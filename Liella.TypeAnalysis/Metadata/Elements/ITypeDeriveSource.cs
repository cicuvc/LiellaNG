namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface ITypeDeriveSource : IEntityEntry
    {
        IEnumerable<ITypeDeriveSource> DerivedType { get; }

    }
}
