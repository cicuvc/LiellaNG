using System.Reflection.Metadata;

namespace Liella.TypeAnalysis.Namespaces
{
    public class TypeNode : NamespaceNodeBase
    {
        public TypeDefinitionHandle TypeDef { get; }
        public override NamespaceNodeBase this[string name, bool isNamespace]
        {
            get => isNamespace ?
                throw new ArgumentException("Namespace cannot be nested") : base[name, isNamespace];
            set => base[name, isNamespace] = isNamespace ?
                throw new ArgumentException("Namespace cannot be nested") : value;
        }
        public string FullName
        {
            get
            {
                var result = new List<string>();
                var currentNode = (NamespaceNodeBase)this;
                while (currentNode is not null)
                {
                    result.Add(currentNode.Name);
                    currentNode = currentNode.Parent;
                }
                result.Reverse();
                return result.Aggregate((u, v) => $"{u}.{v}");
            }
        }
        public TypeNode(string name, NamespaceNodeBase? parent, AssemblyReaderTuple asmInfo, TypeDefinitionHandle typeDef) : base(name, asmInfo, parent)
        {
            TypeDef = typeDef;
        }
    }
}
