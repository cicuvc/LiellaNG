namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IMethodEntry : IEntityGenericContextEntry, ITypeDeriveSource
    {
        public ITypeEntry DeclType { get; }
    }
}
