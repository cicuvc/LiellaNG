using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.TypeAnalysis {
    public static class TopologySort {
        public static ImmutableArray<TNode> Sort<TNode, TEdge>(IEnumerable<TNode> graph) 
            where TNode : GraphNodeBase<TNode, TEdge>
            where TEdge: IGraphEdge<TNode,TEdge> {
            var inEdgeDict = graph.ToKeyFrozenDictionary(e => e, e => e.InEdges.Length);
            var sortQueue = new Queue<TNode>(inEdgeDict.Where(e => e.Value == 0).Select(e => e.Key));
            var result = new TNode[inEdgeDict.Count];
            var resultIndex = 0;

            while(sortQueue.Count != 0) {
                var currentNode = sortQueue.Dequeue();
                result[resultIndex++] = currentNode;

                foreach(var i in currentNode.OutEdges) {
                    if((--inEdgeDict[i.Target]) == 0) {
                        sortQueue.Enqueue(i.Target);
                    }
                }
            }
            return result.ToImmutableArray();
        }
    }
    public interface IGraphEdge<TNode, TEdge>
        where TNode : GraphNodeBase<TNode, TEdge>
        where TEdge: IGraphEdge<TNode, TEdge> {
        TNode Target { get; }
        TEdge ToInvertedEdge(TNode sourceNode);
    }
    public struct DefaultEdge<TNode> : IGraphEdge<TNode, DefaultEdge<TNode>>
        where TNode : GraphNodeBase<TNode, DefaultEdge<TNode>> {
        public DefaultEdge(TNode target) {
            Target = target;
        }
        public TNode Target { get; }

        public DefaultEdge<TNode> ToInvertedEdge(TNode sourceNode) {
            return new(sourceNode);
        }
    }
    public class GraphNodeBase<TNode, TEdge> 
        where TNode: GraphNodeBase<TNode, TEdge>
        where TEdge: IGraphEdge<TNode, TEdge> {
        private TEdge?[] m_InEdges;
        private TEdge?[] m_OutEdges;
        private int m_InEdgeCount;
        private int m_OutEdgeCount;
        public ReadOnlySpan<TEdge> InEdges
           => m_InEdges.AsSpan().Slice(0, m_InEdgeCount);
        public ReadOnlySpan<TEdge> OutEdges
            => m_OutEdges.AsSpan().Slice(0, m_OutEdgeCount);

        public GraphNodeBase() {
            m_InEdges = Array.Empty<TEdge?>();
            m_OutEdges = Array.Empty<TEdge?>();
            m_InEdgeCount = m_OutEdgeCount = 0;
        }
        private static void AddEdgeImpl(ref TEdge?[] edges, ref int length, TEdge value) {
            if(edges.Length == length) {
                var newLength = (length + 2) + (length >> 1);
                Array.Resize(ref edges, newLength);
            }
            edges[length++] = value;
        }
        public void AddEdge(TEdge dstNode) {
            if(dstNode.Target == this) return;
            AddEdgeImpl(ref m_OutEdges, ref m_OutEdgeCount, dstNode);
            AddEdgeImpl(ref dstNode.Target.m_InEdges, ref dstNode.Target.m_InEdgeCount, dstNode.ToInvertedEdge((TNode)this));
        }
    }
    public class GraphNode<T, TEdge>: GraphNodeBase<GraphNode<T, TEdge>, TEdge> 
        where TEdge: IGraphEdge<GraphNode<T,TEdge>, TEdge>
        { 
        public T Value { get; }
        public GraphNode(T container) {
            Value = container;
        }
    }
    public struct TarjanPerNodeInfo {
        private int m_DfsIndex;
        private int m_LowIndex;
        public bool InStack { get => m_DfsIndex < 0; set => m_DfsIndex = ((m_DfsIndex < 0) ^ value) ? -m_DfsIndex : m_DfsIndex; }
        public int LowIndex { get => m_LowIndex; set => m_LowIndex = value; }
        public int DFSTreeIndex { get => Math.Abs(m_DfsIndex); }
        public TarjanPerNodeInfo(int dfsIndex, int lowIndex, bool inStack = true) {
            m_DfsIndex = inStack ? -dfsIndex: dfsIndex;
            m_LowIndex = lowIndex;
        }
    }
    public struct SCCDerivedEdge<TNode, TEdge>
        : IGraphEdge<SCCNode<TNode, TEdge>, SCCDerivedEdge<TNode, TEdge>>
        where TNode : GraphNodeBase<TNode, TEdge>
        where TEdge : IGraphEdge<TNode, TEdge> {
        public SCCNode<TNode, TEdge> Target { get; }
        public int DstNodeIndexInSCC { get; }
        public int DstEdgeIndexInNode { get; }
        public TEdge InternalEdge {
            get {
                var dstNode = Target.Nodes[DstNodeIndexInSCC];
                var invEdge = dstNode.InEdges[DstEdgeIndexInNode];
                return invEdge.ToInvertedEdge(dstNode);
            }
        }
        public SCCDerivedEdge(SCCNode<TNode, TEdge> dstNode, int dstNodeIndexInSCC, int dstEdgeIndexInNode) {
            Target = dstNode;
            DstNodeIndexInSCC = dstNodeIndexInSCC;
            DstEdgeIndexInNode = dstEdgeIndexInNode;
        }

        public SCCDerivedEdge<TNode, TEdge> ToInvertedEdge(SCCNode<TNode, TEdge> sourceNode) {
            var dstNode = Target.Nodes[DstNodeIndexInSCC];
            var invEdge = dstNode.InEdges[DstEdgeIndexInNode];

            var srcNode = invEdge.Target;

            var srcNodeIndex = sourceNode.Nodes.IndexOf(srcNode);
            var srcInEdges = srcNode.InEdges;
            for(var i =0;i< srcInEdges.Length; i++) {
                if(srcInEdges[i].Target == dstNode) {
                    return new(sourceNode, srcNodeIndex, i);
                }
            }

            throw new NotImplementedException();
        }
    }
    public class SCCNode<TNode, TEdge> : GraphNodeBase<SCCNode<TNode, TEdge>, SCCDerivedEdge<TNode, TEdge>>
        where TNode : GraphNodeBase<TNode, TEdge>
        where TEdge: IGraphEdge<TNode, TEdge> {
        public ImmutableArray<TNode> Nodes { get; }
        public SCCNode(ImmutableArray<TNode> subNodes) {
            Nodes = subNodes;
        }
    }
    public class TarjanContext<TNode,TEdge> 
        where TNode:GraphNodeBase<TNode, TEdge>
        where TEdge: IGraphEdge<TNode, TEdge> {
        protected Dictionary<TNode, int> m_NodeDataIndex = new();
        protected Dictionary<TNode, SCCNode<TNode, TEdge>> m_Results = new();
        
        protected Stack<TNode> m_Stack = new();

        protected TarjanPerNodeInfo[] m_NodeData = Array.Empty<TarjanPerNodeInfo>();
        protected List<TNode> m_PopBuffer = new();
        protected int m_NodeCount = 0;

        private static bool DefaultMask(TNode src, TNode dst) => false;
        public int AddNodeData(TNode node, TarjanPerNodeInfo info) {
            if(m_NodeData.Length <= m_NodeCount) {
                var newSize = (m_NodeCount + 2) + (m_NodeCount >> 1);
                Array.Resize(ref m_NodeData, newSize);
            }
            m_NodeDataIndex.Add(node, m_NodeCount);
            m_NodeData[m_NodeCount++] = info;

            return m_NodeCount - 1;
        }
        public ImmutableArray<SCCNode<TNode,TEdge>> Tarjan(IEnumerable<TNode> graphNodes) {
            foreach(var i in graphNodes) {
                if(!m_NodeDataIndex.ContainsKey(i)) TarjanSingle(i);
            }
            foreach(var i in m_Results.Values) {
                foreach(var j in i.Nodes) {
                    foreach(var k in j.OutEdges) {
                        var dstSCC = m_Results[k.Target];
                        var dstNodeIndex = dstSCC.Nodes.IndexOf(k.Target);

                        var inEdges = k.Target.InEdges;
                        var dstEdgeIndex = 0;
                        for(var idx = 0;idx < inEdges.Length; idx++) {
                            if(inEdges[idx].Target == j) {
                                dstEdgeIndex = idx;
                            }
                        }

                        i.AddEdge(new SCCDerivedEdge<TNode, TEdge>(dstSCC, dstNodeIndex, dstEdgeIndex));
                    }
                }
            }

            return m_Results.Values.Distinct().ToImmutableArray();
        }
        public int TarjanSingle(TNode node) {
            var currentIndex = AddNodeData(node, new(m_NodeCount + 1, m_NodeCount + 1));
            m_Stack.Push(node);

            foreach(var i in node.OutEdges) {
                if(!m_NodeDataIndex.TryGetValue(i.Target, out var oldIndex)) {
                    var infoIndex = TarjanSingle(i.Target);
                    m_NodeData[currentIndex].LowIndex =
                        Math.Min(m_NodeData[currentIndex].LowIndex, m_NodeData[infoIndex].LowIndex);
                } else {
                    if(m_NodeData[oldIndex].InStack) {
                        m_NodeData[currentIndex].LowIndex =
                            Math.Min(m_NodeData[currentIndex].LowIndex, m_NodeData[oldIndex].DFSTreeIndex);
                    }
                }
            }

            if(m_NodeData[currentIndex].LowIndex == m_NodeData[currentIndex].DFSTreeIndex) {
                var lastPopNode = default(TNode);

                m_PopBuffer.Clear();

                do {
                    lastPopNode = m_Stack.Pop();
                    m_PopBuffer.Add(lastPopNode);
                    m_NodeData[m_NodeDataIndex[lastPopNode]].InStack = false;
                } while(lastPopNode != node);

                var sccNode = new SCCNode<TNode,TEdge>(m_PopBuffer.ToImmutableArray());

                foreach(var i in sccNode.Nodes) {
                    m_Results.Add(i, sccNode);
                }
            }

            return currentIndex;
        }
    }
}
