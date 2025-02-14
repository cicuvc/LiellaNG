using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Namespaces;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata
{
    public class TypeEnvironment
    {
        protected Dictionary<AssemblyToken, AssemblyReaderTuple> m_Assemblies = new();
        public Dictionary<AssemblyToken, NamespaceQueryTree> NamespaceTree { get; } = new();
        public NamespaceQueryTree? SystemLibraryTree { get; protected set; }
        public EntityEntryManager EntryManager { get; }
        public TypeCollector Collector { get; }
        public MetadataTokenResolver TokenResolver { get; }
        public SignatureDecoder SignDecoder { get; }
        public TypeEnvironment()
        {
            SignDecoder = new(this);
            EntryManager = new(this);
            TokenResolver = new(this);
            Collector = new(this);
        }
        public AssemblyReaderTuple GetAsmInfo(MetadataReader metaReader)
        {
            foreach (var i in m_Assemblies.Values)
                if (i.MetaReader == metaReader)
                    return i;
            throw new NotImplementedException();
        }
        public AssemblyReaderTuple AddMainAssembly(Stream stream)
        {
            var readerTuple = new AssemblyReaderTuple(stream, false);
            m_Assemblies.Add(readerTuple.Token, readerTuple);
            return readerTuple;
        }
        public AssemblyReaderTuple AddDependencyLibrary(Stream stream)
        {
            var readerTuple = new AssemblyReaderTuple(stream, true);
            m_Assemblies.Add(readerTuple.Token, readerTuple);
            return readerTuple;
        }
        public void ImportTypes()
        {
            foreach (var i in m_Assemblies)
            {
                var namespaceTree = new NamespaceQueryTree(i.Value);
                NamespaceTree.Add(i.Key, namespaceTree);
            }
            foreach (var i in m_Assemblies)
            {
                var metaReader = i.Value.MetaReader;
                foreach (var j in metaReader.CustomAttributes)
                {
                    var asmAttribute = metaReader.GetCustomAttribute(j);
                    if (asmAttribute.Parent.Kind != HandleKind.AssemblyDefinition) continue;
                    var attributeConstructor = TokenResolver.ResolveMethodToken(i.Value, asmAttribute.Constructor, GenericTypeContext.EmptyContext, out _);

                    if (attributeConstructor.DeclType.FullName == ".System.Runtime.CompilerServices.SystemLibraryAttribute@0")
                    {
                        if (SystemLibraryTree is not null)
                            throw new InvalidOperationException("Conflict system library");
                        SystemLibraryTree = NamespaceTree[i.Key];
                    }
                }
            }
        }

        public void CollectEntities()
        {
            var initialEntities = NamespaceTree.Where(e =>
            {
                return !e.Value.AssemblyInfo.IsPruneEnabled;
            })
            .SelectMany(e => e.Value.AllTypes)
            .Select(e => TypeDefEntry.Create(EntryManager, e.AsmInfo, e.TypeDef));

            var initialMethods = initialEntities.SelectMany(e => e.GetDetails().Methods);

            Collector.CollectEntities(initialEntities.OfType<IEntityEntry>().Concat(initialMethods));

            Collector.BuildGenericTypeDAG();
        }
    }
}
