using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public class PointerTypeEntry : EntityEntryBase<PointerTypeEntry, PointerTypeEntryTag, EmptyDetails<PointerTypeEntry>>, ITypeEntry, IEntityEntry<PointerTypeEntry>
    {


        public override string Name => $"{InvariantPart.BaseType.Name}*";

        public override string FullName => $"{InvariantPart.BaseType.FullName}*";
        public ITypeEntry? BaseType => null;
        public override bool IsGenericInstantiation => InvariantPart.BaseType.IsGenericInstantiation;

        public override AssemblyReaderTuple AsmInfo => InvariantPart.BaseType.AsmInfo;

        public ImmutableArray<ITypeEntry> TypeArguments => InvariantPart.BaseType.TypeArguments;

        public ImmutableArray<ITypeEntry> MethodArguments => InvariantPart.BaseType.MethodArguments;

        public IEnumerable<ITypeDeriveSource> DerivedType { get; }

        public PointerTypeEntry(TypeEnvironment typeEnv, in PointerTypeEntryTag tag) : base(typeEnv)
        {
            m_InvariantPart = new(tag.BaseType);
            DerivedType = [InvariantPart.BaseType];
        }
        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(InvariantPart.BaseType);
        }

        public MethodDefEntry GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false)
        {
            throw new NotImplementedException();
        }

        public FieldDefEntry GetField(string name)
        {
            throw new NotImplementedException();
        }

        public static PointerTypeEntry CreateFromKey(PointerTypeEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static PointerTypeEntry Create(EntityEntryManager manager, ITypeEntry baseType)
        {
            return CreateEntry(manager, new(baseType));
        }

    }
}
