using System.Runtime.CompilerServices;


namespace Liella.TypeAnalysis.Metadata.Elements
{
    public abstract class EntityEntryBase<TEntry, TInvariantPart, TDetails> : IEntityEntry
        where TEntry : EntityEntryBase<TEntry, TInvariantPart, TDetails>, IEntityEntry<TEntry>
        where TInvariantPart : struct, IEquatable<TInvariantPart>
        where TDetails : struct, IDetails<TEntry>
    {
        [ThreadStatic]
        private static TEntry? m_HashKey;
        protected TInvariantPart m_InvariantPart;
        protected TDetails m_Details;
        public ref TInvariantPart InvariantPart => ref m_InvariantPart;
        public abstract string Name { get; }
        public abstract string FullName { get; }
        public abstract bool IsGenericInstantiation { get; }
        public abstract AssemblyReaderTuple AsmInfo { get; }
        public TypeEnvironment TypeEnv { get; }

        public EntityEntryBase(TypeEnvironment typeEnv) => TypeEnv = typeEnv;
        public bool Equals(IEntityEntry? other)
        {
            if (other is TEntry entry)
                return entry.m_InvariantPart.Equals(m_InvariantPart);
            return false;
        }
        public override int GetHashCode() => m_InvariantPart.GetHashCode();
        protected static TEntry CreateEntry(EntityEntryManager manager, in TInvariantPart invariantPart)
        {
            if (m_HashKey is null)
                m_HashKey = (TEntry)RuntimeHelpers.GetUninitializedObject(typeof(TEntry));
            m_HashKey.m_InvariantPart = invariantPart;
            return manager.GetEntryOrAdd(m_HashKey);
        }

        public abstract void ActivateEntry(TypeCollector collector);
        public ref TDetails GetDetails()
        {
            if (!m_Details.IsValid) m_Details.CreateDetails((TEntry)this);
            return ref m_Details;
        }
        public override string ToString() => FullName;
    }
}
