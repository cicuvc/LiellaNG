using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public sealed class GenericPlaceholderTypeEntry : EntityEntryBase<GenericPlaceholderTypeEntry, GenericPlaceholderTag, GenericPlaceholderDetails>, IEntityEntry<GenericPlaceholderTypeEntry>, ITypeEntry
    {
        public override string Name => $"{InvariantPart.Parent.Name}#{GetDetails().Name}";
        public override string FullName => $"{InvariantPart.Parent.FullName}#{GetDetails().Name}";
        public override bool IsGenericInstantiation => false;
        public override AssemblyReaderTuple AsmInfo => InvariantPart.Parent.AsmInfo;
        public IEntityEntry Parent => InvariantPart.Parent;
        public ITypeEntry? BaseType => null;
        public ImmutableArray<ITypeEntry> TypeArguments => ImmutableArray<ITypeEntry>.Empty;

        public ImmutableArray<ITypeEntry> MethodArguments => ImmutableArray<ITypeEntry>.Empty;

        public IEnumerable<ITypeDeriveSource> DerivedType => Enumerable.Empty<ITypeDeriveSource>();

        private GenericPlaceholderTypeEntry(TypeEnvironment typeEnv, in GenericPlaceholderTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;
        }
        public static GenericPlaceholderTypeEntry CreateFromKey(GenericPlaceholderTypeEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static GenericPlaceholderTypeEntry Create(EntityEntryManager manager, IEntityEntry parent, GenericParameterHandle paramDef)
        {
            return CreateEntry(manager, new(parent, paramDef));
        }

        public MethodDefEntry GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false)
        {
            throw new NotImplementedException();
        }

        public FieldDefEntry GetField(string name)
        {
            throw new NotImplementedException();
        }
        public override void ActivateEntry(TypeCollector collector)
        {
        }
    }
}
