using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public readonly struct MethodDefTag : IEquatable<MethodDefTag>
    {
        public AssemblyReaderTuple AsmInfo { get; }
        public MethodDefinitionHandle MethodDef { get; }
        public MethodDefTag(AssemblyReaderTuple asmInfo, MethodDefinitionHandle methodDef)
        {
            AsmInfo = asmInfo;
            MethodDef = methodDef;
        }
        public bool Equals(MethodDefTag other)
        {
            return other.AsmInfo == AsmInfo && other.MethodDef.Equals(MethodDef);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is MethodDefTag tag && Equals(tag);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
