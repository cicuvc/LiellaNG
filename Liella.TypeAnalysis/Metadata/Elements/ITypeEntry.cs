using System.Reflection.Metadata;
using Liella.TypeAnalysis.Metadata.Entry;


namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface ITypeEntry : IEntityGenericContextEntry, ITypeDeriveSource, IEquatable<IEntityEntry>
    {
        ITypeEntry? BaseType { get; }
        MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false);
        FieldDefEntry GetField(string name);
    }
}
