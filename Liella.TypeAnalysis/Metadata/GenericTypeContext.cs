using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;


namespace Liella.TypeAnalysis.Metadata
{
    public struct GenericTypeContext
    {
        public static GenericTypeContext EmptyContext { get; }
            = new(ImmutableArray<ITypeEntry>.Empty, ImmutableArray<ITypeEntry>.Empty);
        public ImmutableArray<ITypeEntry> TypeArguments { get; }
        public ImmutableArray<ITypeEntry> MethodArguments { get; }
        public GenericTypeContext(ImmutableArray<ITypeEntry> typeArguments, ImmutableArray<ITypeEntry> methodArugments)
        {
            TypeArguments = typeArguments;
            MethodArguments = methodArugments;
        }
    }
}
