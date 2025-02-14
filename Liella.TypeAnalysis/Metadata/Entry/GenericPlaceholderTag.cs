using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct GenericPlaceholderTag : IEquatable<GenericPlaceholderTag>
    {
        public IEntityEntry Parent { get; }
        public GenericParameterHandle ParamDef { get; }
        public GenericPlaceholderTag(IEntityEntry parent, GenericParameterHandle paramDef)
        {
            Parent = parent;
            ParamDef = paramDef;
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Parent);
            hashCode.Add(ParamDef);
            return hashCode.ToHashCode() << 4 | (int)EntryTagType.GenericParamEntry;
        }

        public bool Equals(GenericPlaceholderTag other)
        {
            return other.Parent.Equals(Parent) && other.ParamDef == ParamDef;
        }
    }
}
