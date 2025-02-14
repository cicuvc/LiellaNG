using System.Collections.Frozen;
using System.Reflection.Metadata;

namespace Liella.TypeAnalysis.Namespaces
{
    public class NamespaceQueryTree
    {
        public NamespaceNode RootNamespace { get; }
        public List<TypeNode> AllTypes { get; } = new();
        public FrozenDictionary<TypeDefinitionHandle, TypeNode> TypeNodes { get; }
        public AssemblyReaderTuple AssemblyInfo { get; }
        public NamespaceQueryTree(AssemblyReaderTuple asmInfo)
        {
            AssemblyInfo = asmInfo;
            RootNamespace = new("", asmInfo, null);

            var rootNamespace = asmInfo.MetaReader.GetNamespaceDefinitionRoot();
            ImportNamespace(rootNamespace, RootNamespace);

            TypeNodes = AllTypes.ToFrozenDictionary(e => e.TypeDef);
        }
        public NamespaceNodeBase this[string name, bool isNamespace = true]
        {
            get => RootNamespace[name, isNamespace];
        }
        public TypeNode FindTypeNode(string nsName, string typeName)
        {
            var nsSections = nsName.Split('.');
            var currentNode = (NamespaceNodeBase)RootNamespace;
            foreach (var i in nsSections)
            {
                currentNode = currentNode[i];
            }
            return (TypeNode)currentNode[typeName, false];
        }


        protected void ImportNamespace(NamespaceDefinition nsDef, NamespaceNodeBase currentNode)
        {
            var metaReader = AssemblyInfo.MetaReader;

            foreach (var i in nsDef.NamespaceDefinitions)
            {
                var subNs = metaReader.GetNamespaceDefinition(i);
                var subNsName = metaReader.GetString(subNs.Name);

                if (!currentNode.TryGetNamespace(subNsName, out var subNsNode))
                {
                    currentNode[subNsName, true] = subNsNode = new NamespaceNode(subNsName, AssemblyInfo, currentNode);
                }

                ImportNamespace(subNs, subNsNode!);
            }

            foreach (var i in nsDef.TypeDefinitions)
            {
                var subType = metaReader.GetTypeDefinition(i);
                var subTypeName = metaReader.GetString(subType.Name);

                if (!currentNode.TryGetType(subTypeName, out var subTypeNode))
                {
                    currentNode[subTypeName, false] = subTypeNode = new TypeNode(subTypeName, currentNode, AssemblyInfo, i);
                }

                AllTypes.Add((TypeNode)subTypeNode!);
                ImportType(subType, subTypeNode!);
            }
        }

        protected void ImportType(TypeDefinition nsDef, NamespaceNodeBase currentNode)
        {
            var metaReader = AssemblyInfo.MetaReader;

            foreach (var i in nsDef.GetNestedTypes())
            {
                var subType = metaReader.GetTypeDefinition(i);
                var subTypeName = metaReader.GetString(subType.Name);

                if (!currentNode.TryGetType(subTypeName, out var subTypeNode))
                {
                    currentNode[subTypeName, false] = subTypeNode = new TypeNode(subTypeName, currentNode, AssemblyInfo, i);
                }

                AllTypes.Add((TypeNode)subTypeNode!);
                ImportType(subType, subTypeNode!);
            }
        }
    }
}
