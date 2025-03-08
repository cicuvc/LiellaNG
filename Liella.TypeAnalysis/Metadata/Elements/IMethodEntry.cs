using Liella.TypeAnalysis.Utils;
using System.Reflection;
using System.Reflection.Metadata;

namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IMethodEntry : IEntityGenericContextEntry, ITypeDeriveSource,IAnnotationDecoratable
    {
        public MethodAttributes Attributes { get; }
        public ITypeEntry DeclType { get; }
        public ILDecoder Decoder { get; }
        public MethodSignature<ITypeEntry> Signature { get; }
        public IMethodEntry? VirtualMethodPrototype { get; }
        public MethodImplAttributes ImplAttributes { get; }
    }
}
