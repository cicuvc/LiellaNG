using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Utils;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public sealed class MethodInstantiation : EntityEntryBase<MethodInstantiation, MethodInstantiationTag, EmptyDetails<MethodInstantiation>>, IMethodEntry, IEntityEntry<MethodInstantiation>, IInstantiationEntry
    {
        public string TypeParams => InvariantPart.MethodArguments.Select(e => e.Name).DefaultIfEmpty("").Aggregate((u, v) => $"{u},{v}");
        public override string Name => $"{InvariantPart.Definition.Name}[{TypeParams}]";
        public override string FullName => $"{InvariantPart.ExactDeclType.FullName}::{InvariantPart.Definition.Name}[{TypeParams}]";

        public override bool IsGenericInstantiation => true;

        public override AssemblyReaderTuple AsmInfo => DeclType.AsmInfo;

        public ITypeEntry DeclType => InvariantPart.Definition.DeclType;
        public ITypeEntry ExactDeclType => InvariantPart.ExactDeclType;

        public ImmutableArray<ITypeEntry> TypeArguments => InvariantPart.ExactDeclType.TypeArguments;

        public ImmutableArray<ITypeEntry> MethodArguments => InvariantPart.MethodArguments;

        public IEnumerable<ITypeDeriveSource> DerivedType => Enumerable.Empty<ITypeDeriveSource>();

        public IEnumerable<ITypeEntry> FormalArguments => InvariantPart.Definition.MethodArguments;

        public IEnumerable<ITypeEntry> ActualArguments => InvariantPart.MethodArguments;

        public IEntityEntry Definition => InvariantPart.Definition;
        public ILDecoder Decoder => InvariantPart.Definition.Decoder;
        public MethodSignature<ITypeEntry> Signature => InvariantPart.Definition.Signature;
        public MethodAttributes Attributes => InvariantPart.Definition.Attributes;
        public MethodImplAttributes ImplAttributes => InvariantPart.Definition.ImplAttributes;
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes
            => InvariantPart.Definition.CustomAttributes;
        //public int TypeArgumentCount => TypeArguments.Length;
        //public int MethodArgumentCount => MethodArguments.Length;
        public bool IsPrimary => true;
        public bool IsTypeInst => false;

        public IMethodEntry? VirtualMethodPrototype => InvariantPart.Definition.VirtualMethodPrototype;

        public IEnumerable<ITypeEntry> LocalVariableTypes => InvariantPart.Definition.LocalVariableTypes;

        public MethodInstantiation(TypeEnvironment typeEnv, in MethodInstantiationTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;

        }

        public static MethodInstantiation CreateFromKey(MethodInstantiation key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static MethodInstantiation Create(EntityEntryManager manager, ITypeEntry exactType, MethodDefEntry definition, ImmutableArray<ITypeEntry> methodArguments)
        {
            return CreateEntry(manager, new(exactType, definition, methodArguments));
        }

        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(InvariantPart.Definition);
        }

        public IInstantiationEntry AsPrimary(EntityEntryManager _, bool isPrimary = true) => this;

        public GenericTypeContext GetGenericContext() {
            return new(TypeArguments, MethodArguments);
        }
    }
}
