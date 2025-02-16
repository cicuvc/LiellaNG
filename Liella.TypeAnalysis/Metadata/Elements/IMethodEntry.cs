namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IMethodEntry : IEntityGenericContextEntry, ITypeDeriveSource,IAnnotationDecoratable
    {
        public ITypeEntry DeclType { get; }
    }
}
