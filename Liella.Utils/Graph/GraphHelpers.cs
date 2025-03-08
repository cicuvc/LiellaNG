using System.Collections.Immutable;

namespace Liella.TypeAnalysis.Utils.Graph
{
    public static class GraphHelpers
    {
        public static IEnumerable<TNode> TopoSort<TNode, TExtra>(FwdGraph<TNode, TExtra> graph) where TNode : class
        {
            var inEdgeCounts = graph.ToKeyFrozenDictionary(e => e, e => graph.GetInvEdge(e).Count());
            var queue = new Queue<TNode>();
            var result = new List<TNode>();

            foreach (var (k, v) in inEdgeCounts)
            {
                if (v == 0) queue.Enqueue(k);
            }

            while (queue.Count != 0)
            {
                var currentNode = queue.Dequeue();
                result.Add(currentNode);

                foreach (var i in graph.GetForwardEdge(currentNode))
                {
                    if (0 == --inEdgeCounts[i.Target])
                    {
                        queue.Enqueue(i.Target);
                    }
                }
            }

            return result;
        }

        private struct TarjanPerNodeInfo
        {
            public int DFSIndex { get; private set; }
            public int LowIndex { get; private set; }
            public TarjanPerNodeInfo(int initDfn)
            {
                DFSIndex = LowIndex = initDfn;
            }
            public TarjanPerNodeInfo(int dfn, int low)
            {
                DFSIndex = dfn;
                LowIndex = low;
            }
            public TarjanPerNodeInfo UpdateLow(int newValue)
            {
                return new TarjanPerNodeInfo(DFSIndex, Math.Min(newValue, LowIndex));
            }
        }
        public static FwdGraph<SCCNode<TNode>, FwdGraph<TNode, TExtraData>.Edge> Tarjan<TNode, TExtraData>(FwdGraph<TNode, TExtraData> graph) where TNode : class
        {
            var nodeIndex = new Dictionary<TNode, int>();
            var results = new Dictionary<TNode, SCCNode<TNode>>();
            var perNodeData = new (int low, int dfn, bool inStack)[graph.NodeCount];
            var stack = new Stack<TNode>();
            var nodeInitIndex = 0;
            var popBuffer = new List<TNode>();
            var finalGraph = new FwdGraph<SCCNode<TNode>, FwdGraph<TNode, TExtraData>.Edge>();

            var index = 0;
            foreach (var i in graph) nodeIndex.Add(i, index++);


            void TarjanImpl(TNode node)
            {
                var nodeCurrentIndex = nodeIndex[node];
                perNodeData[nodeCurrentIndex].dfn = perNodeData[nodeCurrentIndex].low = ++nodeInitIndex;
                perNodeData[nodeCurrentIndex].inStack = true;
                stack.Push(node);

                foreach (var i in graph.GetForwardEdge(node))
                {
                    var targetIndex = nodeIndex[i.Target];

                    if (perNodeData[targetIndex].dfn == 0)
                    {
                        TarjanImpl(i.Target);

                        perNodeData[nodeCurrentIndex].low = Math.Min(perNodeData[nodeCurrentIndex].low, perNodeData[targetIndex].low);
                    }
                    else
                    {
                        if (perNodeData[targetIndex].inStack)
                        {
                            perNodeData[nodeCurrentIndex].low = Math.Min(perNodeData[nodeCurrentIndex].low, perNodeData[targetIndex].dfn);
                        }
                    }
                }

                if (perNodeData[nodeCurrentIndex].low == perNodeData[nodeCurrentIndex].dfn)
                {
                    var lastPopNode = default(TNode);

                    popBuffer.Clear();

                    do
                    {
                        lastPopNode = stack.Pop();
                        popBuffer.Add(lastPopNode);
                        perNodeData[nodeIndex[lastPopNode]].inStack = false;
                    } while (lastPopNode != node);

                    var sccNode = new SCCNode<TNode>(popBuffer.ToImmutableArray());

                    foreach (var i in sccNode.InternalNodes)
                    {
                        results.Add(i, sccNode);
                    }
                }

            }


            foreach (var i in graph)
            {
                if (perNodeData[nodeIndex[i]].dfn == 0) TarjanImpl(i);
            }

            var deduplicationSet = new HashSet<(SCCNode<TNode>, SCCNode<TNode>)>();
            foreach (var i in results)
            {
                foreach (var j in i.Value.InternalNodes)
                {
                    var jScc = results[j];
                    foreach (var k in graph.GetForwardEdge(j))
                    {
                        var kScc = results[k.Target];
                        if (kScc == jScc) continue;

                        if(!deduplicationSet.Contains((jScc, kScc))) {
                            deduplicationSet.Add((jScc, kScc));
                            finalGraph.AddEdge(jScc, kScc, new(j, k.Target, k.ExtraData));
                        }
                        
                    }
                }
            }

            return finalGraph;
        }
    }
}
