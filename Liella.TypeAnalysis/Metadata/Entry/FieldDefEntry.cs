using Liella.TypeAnalysis.Metadata.Elements;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public class FieldDefEntry : EntityEntryBase<FieldDefEntry, FieldDefEntryTag, FieldDetails>, IEntityEntry<FieldDefEntry>, IEntityEntry
    {
        public override string Name => GetDetails().Name;
        public override string FullName => $"{GetDetails().DeclType.FullName}::{GetDetails().Name}";
        public override bool IsGenericInstantiation => false;
        public override AssemblyReaderTuple AsmInfo => InvariantPart.AsmInfo;
        public FieldDefEntry(TypeEnvironment typeEnv, in FieldDefEntryTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;
        }
        public static FieldDefEntry CreateFromKey(FieldDefEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static FieldDefEntry Create(EntityEntryManager manager, AssemblyReaderTuple asmInfo, FieldDefinitionHandle fieldDef)
        {
            return CreateEntry(manager, new(asmInfo, fieldDef));
        }
        public override void ActivateEntry(TypeCollector collector)
        {
            collector.NotifyEntity(GetDetails().FieldType);
        }
    }
}
