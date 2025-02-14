using System.Collections.Immutable;


namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IEntityGenericContextEntry : IEntityEntry
    {
        public ImmutableArray<ITypeEntry> TypeArguments { get; }
        public ImmutableArray<ITypeEntry> MethodArguments { get; }
    }
}
