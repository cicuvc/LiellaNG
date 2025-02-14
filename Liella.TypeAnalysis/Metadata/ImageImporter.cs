namespace Liella.TypeAnalysis.Metadata
{
    public class ImageImporter
    {
        public TypeEnvironment TypeEnv { get; } = new();
        protected Func<AssemblyToken, Stream> m_ResolveCallback;
        public ImageImporter(Stream mainAssembly, Func<AssemblyToken, Stream> resolveCallback)
        {
            m_ResolveCallback = resolveCallback;

            var readerTuple = TypeEnv.AddMainAssembly(mainAssembly);
            ResolveDependency(readerTuple);
        }

        protected void ResolveDependency(AssemblyReaderTuple asmInfo)
        {
            foreach (var i in asmInfo.MetaReader.AssemblyReferences)
            {
                var asmRef = asmInfo.MetaReader.GetAssemblyReference(i);

                var asmFullName = new AssemblyToken(asmInfo.MetaReader, asmRef);

                var dependencyStream = m_ResolveCallback(asmFullName);

                var dependencyTuple = TypeEnv.AddDependencyLibrary(dependencyStream);

                ResolveDependency(dependencyTuple);
            }
        }

        public void BuildTypeEnvironment()
        {
            TypeEnv.ImportTypes();
            TypeEnv.CollectEntities();
        }
    }
}
