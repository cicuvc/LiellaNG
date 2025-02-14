using System.Collections.Immutable;

namespace Liella.TypeAnalysis.Utils.Graph {
    public class SCCNode<TNode>
    {
        public ImmutableArray<TNode> InternalNodes { get; }
        public SCCNode(ImmutableArray<TNode> internalNoeds)
        {
            InternalNodes = internalNoeds;
        }
    }
}
