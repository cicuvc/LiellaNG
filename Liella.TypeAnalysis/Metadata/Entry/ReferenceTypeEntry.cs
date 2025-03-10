using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public class ReferenceTypeEntry : EntityEntryBase<ReferenceTypeEntry, ReferenceTypeEntryTag, EmptyDetails<ReferenceTypeEntry>>, ITypeEntry, IEntityEntry<ReferenceTypeEntry>,ITypeInstEntry {
        protected static SecondaryTypePhonyGenericPlaceholder m_ReferenceTypePlaceholder = new("TReferenceTypeEntry");

        public override string Name => $"{InvariantPart.ElementType.Name}&";

        public override string FullName => $"{InvariantPart.ElementType.FullName}&";
        public ITypeEntry? BaseType => null;
        public override bool IsGenericInstantiation => true;

        public override AssemblyReaderTuple AsmInfo => InvariantPart.ElementType.AsmInfo;

        public ImmutableArray<ITypeEntry> TypeArguments => InvariantPart.ElementType.TypeArguments;

        public ImmutableArray<ITypeEntry> MethodArguments => InvariantPart.ElementType.MethodArguments;

        public IEnumerable<ITypeDeriveSource> DerivedType { get; }
        public TypeAttributes Attributes => (TypeAttributes)0;
        public bool IsValueType => true;
        public IEnumerable<MethodDefEntry> TypeMethods => throw new NotSupportedException();

        public IEnumerable<FieldDefEntry> TypeFields => throw new NotSupportedException();
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes
            => ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)>.Empty;
        public IReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>> ImplInterfaces 
            => ReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>.Empty;

        public IEnumerable<ITypeEntry> FormalArguments => [m_ReferenceTypePlaceholder];

        public IEnumerable<ITypeEntry> ActualArguments => [m_InvariantPart.ElementType];

        public IEntityEntry? Definition => null;

        public bool IsPrimary => true;

        public bool IsTypeInst => true;

        public ReferenceTypeEntry(TypeEnvironment typeEnv, in ReferenceTypeEntryTag tag) : base(typeEnv) {
            m_InvariantPart = new(tag.ElementType);
            DerivedType = [InvariantPart.ElementType];
        }
        public override void ActivateEntry(TypeCollector collector) {
            collector.NotifyEntity(InvariantPart.ElementType);
        }

        public MethodDefEntry GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false) {
            throw new NotImplementedException();
        }

        public FieldDefEntry GetField(string name) {
            throw new NotImplementedException();
        }

        public static ReferenceTypeEntry CreateFromKey(ReferenceTypeEntry key, TypeEnvironment typeEnv) {
            return new(typeEnv, key.InvariantPart);
        }
        public static ReferenceTypeEntry Create(EntityEntryManager manager, ITypeEntry baseType) {
            if(baseType is ReferenceTypeEntry refType) return refType;
            return CreateEntry(manager, new(baseType));
        }

        public IInstantiationEntry AsPrimary(EntityEntryManager manager, bool isPrimary = true) {
            return this;
        }
    }
}
