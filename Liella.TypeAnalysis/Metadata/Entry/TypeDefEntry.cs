using Liella.TypeAnalysis.Metadata.Elements;
using System.Collections.Immutable;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public sealed class TypeDefEntry : EntityEntryBase<TypeDefEntry, TypeDefEntryTag, TypeDefDetails>, IEntityEntry<TypeDefEntry>, ITypeEntry
    {
        public string TypeParams => TypeArguments.Length == 0 ? "" : $"[{TypeArguments.Select(e => e.Name).DefaultIfEmpty("").Aggregate((u, v) => $"{u},{v}")}]";
        public override string Name => $"{GetDetails().Name}@{TypeArguments.Length}";
        public override string FullName => $"{GetDetails().Prototype.FullName}@{TypeArguments.Length}";
        public override bool IsGenericInstantiation => false;
        public override AssemblyReaderTuple AsmInfo => m_InvariantPart.AsmInfo;

        public ImmutableArray<ITypeEntry> TypeArguments => GetDetails().TypeArguments;
        public ImmutableArray<ITypeEntry> MethodArguments => ImmutableArray<ITypeEntry>.Empty;
        public ITypeEntry? BaseType => GetDetails().BaseType;
        public IEnumerable<ITypeDeriveSource> DerivedType => GetDetails().DerivedEntry;
        public ImmutableArray<MethodDefEntry> Methods => GetDetails().Methods;
        private TypeDefEntry(TypeEnvironment typeEnv, in TypeDefEntryTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;
        }
        public static TypeDefEntry CreateFromKey(TypeDefEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static TypeDefEntry Create(EntityEntryManager manager, AssemblyReaderTuple asmInfo, TypeDefinitionHandle typeDef)
        {
            return CreateEntry(manager, new(asmInfo, typeDef));
        }

        public MethodDefEntry? GetMethod(string name, in MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext, bool throwOnNotFound = false)
        {
            foreach (var i in GetDetails().Methods)
            {
                if (i.Name != name) continue;
                if (i.IsMatchSignature(signature, genericContext))
                {
                    return i;
                }
            }
            if (throwOnNotFound) throw new KeyNotFoundException();
            return null;
        }

        public FieldDefEntry GetField(string name)
        {
            foreach (var i in GetDetails().Fields)
            {
                if (i.Name == name) return i;
            }
            throw new KeyNotFoundException();
        }

        public override void ActivateEntry(TypeCollector collector)
        {
            foreach (var i in GetDetails().DerivedEntry)
                collector.NotifyEntity(i);
        }
    }
}
