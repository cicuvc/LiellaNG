using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public class PrimitiveTypeEntry : EntityEntryBase<PrimitiveTypeEntry, PrimitiveTypeEntryTag, PrimitiveTypeEntryDetails>, IEntityEntry<PrimitiveTypeEntry>, ITypeEntry
    {
        public override string Name => $"Primitive_{InvariantPart.TypeCode}";

        public override string FullName => $"Primitive_{InvariantPart.TypeCode}";

        public override bool IsGenericInstantiation => false;



        public override AssemblyReaderTuple AsmInfo => GetDetails().DefinitionType.AsmInfo;

        public ITypeEntry? BaseType => GetDetails().DefinitionType.BaseType;

        public ImmutableArray<ITypeEntry> TypeArguments => ImmutableArray<ITypeEntry>.Empty;

        public ImmutableArray<ITypeEntry> MethodArguments => ImmutableArray<ITypeEntry>.Empty;

        public IEnumerable<ITypeDeriveSource> DerivedType => Enumerable.Empty<ITypeDeriveSource>();

        public TypeAttributes Attributes => TypeAttributes.Class | TypeAttributes.SpecialName | TypeAttributes.RTSpecialName;

        public bool IsValueType => true;
        public IEnumerable<MethodDefEntry> TypeMethods => throw new NotSupportedException();
        public IEnumerable<FieldDefEntry> TypeFields => throw new NotSupportedException();

        public PrimitiveTypeEntry(TypeEnvironment typeEnv, in PrimitiveTypeEntryTag code) : base(typeEnv)
        {
            m_InvariantPart = code;
        }

        public static PrimitiveTypeEntry CreateFromKey(PrimitiveTypeEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static PrimitiveTypeEntry Create(EntityEntryManager manager, PrimitiveTypeCode code)
        {
            return CreateEntry(manager, new(code));
        }
        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(GetDetails().DefinitionType);
        }

        public MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false)
        {
            return GetDetails().DefinitionType.GetMethod(name, signature, genericContext, throwOnNotFound);
        }

        public FieldDefEntry GetField(string name)
        {
            return GetDetails().DefinitionType.GetField(name);
        }
    }
}
