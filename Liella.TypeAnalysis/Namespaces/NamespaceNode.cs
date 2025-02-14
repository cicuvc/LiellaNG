namespace Liella.TypeAnalysis.Namespaces
{
    public class NamespaceNode : NamespaceNodeBase
    {
        public NamespaceNode(string name, AssemblyReaderTuple asmInfo, NamespaceNodeBase? parent) : base(name, asmInfo, parent)
        {
        }
    }
}
