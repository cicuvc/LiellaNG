using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct MethodInstantiationTag : IEquatable<MethodInstantiationTag>
    {
        public ITypeEntry ExactDeclType { get; }
        public MethodDefEntry Definition { get; }
        public ImmutableArray<ITypeEntry> MethodArguments { get; }
        public MethodInstantiationTag(ITypeEntry exactDeclType, MethodDefEntry definition, ImmutableArray<ITypeEntry> methodArguments)
        {
            ExactDeclType = exactDeclType;
            Definition = definition;
            MethodArguments = methodArguments;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(ExactDeclType);
            hashCode.Add(Definition);
            hashCode.Add(MethodArguments);
            return hashCode.ToHashCode() << 4 | (int)EntryTagType.MethodInstEntry;
        }

        public bool Equals(MethodInstantiationTag other)
        {
            return other.ExactDeclType == ExactDeclType &&
                other.Definition.Equals(Definition) &&
                other.MethodArguments.SequenceEqual(MethodArguments);
        }
    }
}
