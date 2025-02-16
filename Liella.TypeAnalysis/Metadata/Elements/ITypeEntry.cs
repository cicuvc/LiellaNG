using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Liella.TypeAnalysis.Metadata.Entry;


namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IAnnotationDecoratable {
        ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes { get; }
    }
    public interface ITypeEntry : IEntityGenericContextEntry, ITypeDeriveSource, IEquatable<IEntityEntry>, IAnnotationDecoratable {
        TypeAttributes Attributes { get; }
        ITypeEntry? BaseType { get; }

        bool IsValueType { get; }
        IEnumerable<MethodDefEntry> TypeMethods { get; }
        IEnumerable<FieldDefEntry> TypeFields { get; }
        MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false);
        FieldDefEntry GetField(string name);
    }
}
