namespace Liella.TypeAnalysis.Metadata.Elements
{
    public interface ITypeInstEntry: IInstantiationEntry { }
    public interface IInstantiationEntry : IEntityEntry
    {
        IEnumerable<ITypeEntry> FormalArguments { get; }
        IEnumerable<ITypeEntry> ActualArguments { get; }
        IEntityEntry? Definition { get; }
        //int TypeArgumentCount { get; }
        //int MethodArgumentCount { get; }
        bool IsPrimary { get; }
        bool IsTypeInst { get; }
        IInstantiationEntry AsPrimary(EntityEntryManager manager);
        public int ArgumentCount => FormalArguments.Count();
        //public IEnumerable<ITypeEntry> IntrinsicFormalArgument => IsTypeInst ? FormalArguments : FormalArguments.TakeLast(MethodArgumentCount);
        //public IEnumerable<ITypeEntry> IntrinsicActualArgument => IsTypeInst ? ActualArguments : ActualArguments.TakeLast(MethodArgumentCount);
        //public int IntrinsicArgumentCount => IsTypeInst ? TypeArgumentCount : MethodArgumentCount;

    }
}
