namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface IEntityEntry : IEquatable<IEntityEntry>
    {
        string Name { get; }
        string FullName { get; }
        bool IsGenericInstantiation { get; }
        TypeEnvironment TypeEnv { get; }
        AssemblyReaderTuple AsmInfo { get; }
        void ActivateEntry(TypeCollector collector);
    }
    public interface IEntityEntry<T> : IEntityEntry where T : IEntityEntry<T>
    {
        static abstract T CreateFromKey(T key, TypeEnvironment typeEnv);
    }
}
