using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public sealed class TypeInstantiationEntry : EntityEntryBase<TypeInstantiationEntry, TypeInstantiationEntryTag, EmptyDetails<TypeInstantiationEntry>>, ITypeEntry, IEntityEntry<TypeInstantiationEntry>, IInstantiationEntry
    {
        public string GenericTypeArguments => InvariantPart.TypeArguments.Select(e => e.Name).DefaultIfEmpty("").Aggregate((u, v) => u + "," + v);
        public override string Name => $"{InvariantPart.DefinitionType.Name}[{GenericTypeArguments}]";
        public override string FullName => $"{InvariantPart.DefinitionType.FullName}[{GenericTypeArguments}]";
        public override bool IsGenericInstantiation => true;
        public override AssemblyReaderTuple AsmInfo => InvariantPart.DefinitionType.AsmInfo;

        public ImmutableArray<ITypeEntry> TypeArguments => InvariantPart.TypeArguments;
        public ImmutableArray<ITypeEntry> MethodArguments => ImmutableArray<ITypeEntry>.Empty;

        public IEnumerable<ITypeDeriveSource> DerivedType => Enumerable.Empty<ITypeDeriveSource>();

        public IEnumerable<ITypeEntry> FormalArguments => InvariantPart.DefinitionType.TypeArguments;
        public IEnumerable<ITypeEntry> ActualArguments => InvariantPart.TypeArguments;

        public IEntityEntry Definition => InvariantPart.DefinitionType;
        public ITypeEntry? BaseType => InvariantPart.DefinitionType.BaseType;
        //public int TypeArgumentCount => TypeArguments.Length;
        //public int MethodArgumentCount => 0;
        public bool IsPrimary => InvariantPart.IsPrimary;
        public bool IsTypeInst => true;
        public TypeAttributes Attributes => ((ITypeEntry)Definition).Attributes;
        public bool IsValueType => ((ITypeEntry)Definition).IsValueType;

        public IEnumerable<MethodDefEntry> TypeMethods => InvariantPart.DefinitionType.TypeMethods;

        public IEnumerable<FieldDefEntry> TypeFields => InvariantPart.DefinitionType.TypeFields;
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes
            => InvariantPart.DefinitionType.CustomAttributes;
        public TypeInstantiationEntry(TypeEnvironment typeEnv, in TypeInstantiationEntryTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;
        }

        public static TypeInstantiationEntry CreateFromKey(TypeInstantiationEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static TypeInstantiationEntry Create(EntityEntryManager manager, TypeDefEntry definition, ImmutableArray<ITypeEntry> typeArguments, bool isPrimary)
        {
            return CreateEntry(manager, new(definition, typeArguments, isPrimary));
        }

        public MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false)
        {
            return InvariantPart.DefinitionType.GetMethod(name, signature, genericContext, throwOnNotFound);
        }

        public FieldDefEntry GetField(string name)
        {
            return InvariantPart.DefinitionType.GetField(name);
        }

        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(InvariantPart.DefinitionType);
        }

        public IInstantiationEntry AsPrimary(EntityEntryManager manager)
        {
            return Create(manager, InvariantPart.DefinitionType, TypeArguments, true);
        }
    }
}
