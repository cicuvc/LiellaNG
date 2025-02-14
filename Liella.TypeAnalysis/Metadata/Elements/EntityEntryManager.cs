namespace Liella.TypeAnalysis.Metadata.Elements
{
    public class EntityEntryManager
    {
        public TypeEnvironment TypeEnv { get; }
        protected HashSet<IEntityEntry> m_EntrySet = new();
        public EntityEntryManager(TypeEnvironment typeEnv)
        {
            TypeEnv = typeEnv;
        }
        public T GetEntryOrAdd<T>(T key) where T : IEntityEntry<T>
        {
            if (!m_EntrySet.TryGetValue(key, out var actualValue))
            {
                m_EntrySet.Add(actualValue = T.CreateFromKey(key, TypeEnv));
            }
            return (T)actualValue;
        }
    }
}
