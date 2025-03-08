using Liella.TypeAnalysis.Metadata.Elements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct ArrayTypeEntryTag : IEquatable<ArrayTypeEntryTag> {
        public ITypeEntry ElementType { get; }
        public ArrayTypeEntryTag(ITypeEntry element) {
            ElementType = element;
        }
        public bool Equals(ArrayTypeEntryTag other) {
            return other.ElementType == ElementType;
        }
    }
    public class ArrayTypeEntry : EntityEntryBase<ArrayTypeEntry, ArrayTypeEntryTag, EmptyDetails<ArrayTypeEntry>>, IEntityEntry<ArrayTypeEntry>, ITypeEntry {
        public ArrayTypeEntry(TypeEnvironment typeEnv, in ArrayTypeEntryTag tag) : base(typeEnv) {
            m_InvariantPart = tag;
        }

        protected TypeDefEntry ResolveArrayBaseType() {
            return TypeEnv.ResolveSystemTypeFromFullName("System.Array");
        }
        public TypeAttributes Attributes => TypeAttributes.Class;

        public ITypeEntry? BaseType => ResolveArrayBaseType();

        public bool IsValueType => false;

        public IEnumerable<MethodDefEntry> TypeMethods => [];

        public IEnumerable<FieldDefEntry> TypeFields => [];

        public IReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>> ImplInterfaces
            => ReadOnlyDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>.Empty;

        public ImmutableArray<ITypeEntry> TypeArguments => [];

        public ImmutableArray<ITypeEntry> MethodArguments => [];

        public IEnumerable<ITypeDeriveSource> DerivedType => [m_InvariantPart.ElementType];

        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes => [];

        public override string Name => throw new NotImplementedException();

        public override string FullName => throw new NotImplementedException();

        public override bool IsGenericInstantiation => throw new NotImplementedException();

        public override AssemblyReaderTuple AsmInfo => throw new NotImplementedException();

        public static ArrayTypeEntry CreateFromKey(ArrayTypeEntry key, TypeEnvironment typeEnv) {
            return new(typeEnv, key.InvariantPart);
        }
        public static ArrayTypeEntry Create(EntityEntryManager manager, ITypeEntry elementType) {
            return CreateEntry(manager, new(elementType));
        }
        public FieldDefEntry GetField(string name) {
            return ResolveArrayBaseType().GetField(name);
        }

        public MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false) {
            return ResolveArrayBaseType().GetMethod(name, signature, genericContext, throwOnNotFound);
        }

        public override void ActivateEntry(TypeCollector collector) {
            collector.NotifyEntity(m_InvariantPart.ElementType);
        }
    }
}
