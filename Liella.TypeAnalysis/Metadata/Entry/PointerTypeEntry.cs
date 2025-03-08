using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry {
    public class SecondaryTypePhonyGenericPlaceholder : IEntityEntry, ITypeEntry, IGenericPlaceholder {
        public string Name { get; }

        public string FullName => Name;

        public bool IsGenericInstantiation => false;

        public TypeEnvironment TypeEnv => throw new NotImplementedException();

        public AssemblyReaderTuple AsmInfo => throw new NotImplementedException();

        public TypeAttributes Attributes => 0;

        public ITypeEntry? BaseType => null;

        public bool IsValueType => false;

        public IEnumerable<MethodDefEntry> TypeMethods => [];

        public IEnumerable<FieldDefEntry> TypeFields => [];

        public IReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>> ImplInterfaces
            => ReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>.Empty;

        public ImmutableArray<ITypeEntry> TypeArguments => [];

        public ImmutableArray<ITypeEntry> MethodArguments => [];

        public IEnumerable<ITypeDeriveSource> DerivedType => [];

        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes => [];

        public SecondaryTypePhonyGenericPlaceholder(string name) {
            Name = name;
        }
        public void ActivateEntry(TypeCollector collector) {
            
        }

        public bool Equals(IEntityEntry? other) {
            return other == this;
        }

        public MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false) {
            throw new NotImplementedException();
        }

        public FieldDefEntry GetField(string name) {
            throw new NotImplementedException();
        }
    }
    public class PointerTypeEntry : EntityEntryBase<PointerTypeEntry, PointerTypeEntryTag, EmptyDetails<PointerTypeEntry>>, ITypeEntry, IEntityEntry<PointerTypeEntry>, ITypeInstEntry
    {
        protected static SecondaryTypePhonyGenericPlaceholder m_Placeholder = new("TPointerType");
        public override string Name => $"{InvariantPart.ElementType.Name}*";

        public override string FullName => $"{InvariantPart.ElementType.FullName}*";
        public ITypeEntry? BaseType => null;
        public override bool IsGenericInstantiation => true;

        public override AssemblyReaderTuple AsmInfo => InvariantPart.ElementType.AsmInfo;

        public ImmutableArray<ITypeEntry> TypeArguments => InvariantPart.ElementType.TypeArguments;

        public ImmutableArray<ITypeEntry> MethodArguments => InvariantPart.ElementType.MethodArguments;

        public IEnumerable<ITypeDeriveSource> DerivedType { get; }
        public TypeAttributes Attributes => (TypeAttributes)0;
        public bool IsValueType => true;
        public IEnumerable<MethodDefEntry> TypeMethods => [];

        public IEnumerable<FieldDefEntry> TypeFields => [];
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes
            => ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)>.Empty;

        public IReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>> ImplInterfaces 
            => ReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>.Empty;

        public IEnumerable<ITypeEntry> FormalArguments => [m_Placeholder];

        public IEnumerable<ITypeEntry> ActualArguments => [m_InvariantPart.ElementType];

        public IEntityEntry? Definition => null!;

        public bool IsPrimary => true; // Pointer/Reference cannot be generic parameter so always primary
        public bool IsTypeInst => true;

        public PointerTypeEntry(TypeEnvironment typeEnv, in PointerTypeEntryTag tag) : base(typeEnv)
        {
            m_InvariantPart = new(tag.ElementType);
            DerivedType = [InvariantPart.ElementType];
        }
        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(InvariantPart.ElementType);
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

        public IInstantiationEntry AsPrimary(EntityEntryManager manager, bool isPrimary = true) {
            return this;
        }
    }
}
