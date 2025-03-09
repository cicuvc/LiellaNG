using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Namespaces;
using Liella.TypeAnalysis.Utils;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata
{
    public class TypeEnvironment
    {
        public ImmutableArray<string> BuiltinTypes { get; }
        protected Dictionary<AssemblyToken, AssemblyReaderTuple> m_Assemblies = new();
        public Dictionary<AssemblyToken, NamespaceQueryTree> NamespaceTree { get; } = new();
        public NamespaceQueryTree? SystemLibraryTree { get; protected set; }
        public EntityEntryManager EntryManager { get; }
        public TypeCollector Collector { get; }
        public MetadataTokenResolver TokenResolver { get; }
        public SignatureDecoder SignDecoder { get; }
        public TypeEnvironment(ImmutableArray<string> builtinTypes)
        {
            BuiltinTypes = builtinTypes;
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
            .Where(e => {
                var typeDef = e.AsmInfo.MetaReader.GetTypeDefinition(e.TypeDef);
                foreach(var i in typeDef.GetCustomAttributes()) {
                    var custAttribute = e.AsmInfo.MetaReader.GetCustomAttribute(i);
                    var constructor = TokenResolver.ResolveMethodToken(e.AsmInfo, custAttribute.Constructor, GenericTypeContext.EmptyContext, out _); ;
                    if(constructor.FullName == ".System.NoPruningAttribute@0::.ctor") return true;
                }
                return false;
            })
            .Select(e => TypeDefEntry.Create(EntryManager, e.AsmInfo, e.TypeDef))
            .Concat(Enum.GetValues<PrimitiveTypeCode>().Select(ResolvePrimitiveType))
            .Concat(BuiltinTypes.Select(ResolveSystemTypeFromFullName));

            var initialMethods = initialEntities.SelectMany(e => e.GetDetails().Methods);

            Collector.CollectEntities(initialEntities.OfType<IEntityEntry>().Concat(initialMethods));

            Collector.BuildGenericTypeDAG();
        }

        public TypeDefEntry ResolvePrimitiveType(PrimitiveTypeCode code) {
            var typeNode = TokenResolver.ResolvePrimitiveType(code);
            return TypeDefEntry.Create(EntryManager, typeNode.AsmInfo, typeNode.TypeDef);
        }

        public TypeDefEntry ResolveSystemTypeFromFullName(string name) {
            NamespaceNodeBase currentNode = SystemLibraryTree!.RootNamespace;
            var sections = name.Split('.');
            foreach(var i in sections[..^1]) {
                currentNode = currentNode[i];
            }
            var typeNode = (TypeNode)currentNode[sections[^1], false];
            return TypeDefEntry.Create(EntryManager, typeNode.AsmInfo, typeNode.TypeDef);
        }
    }
}
