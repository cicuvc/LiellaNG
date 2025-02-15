using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Liella.TypeAnalysis.Utils.Graph {
    public struct EmptyExtraData { }
    public class FwdGraph<TNode, TEdgeExtraData> : IEnumerable<TNode>
        where TNode : class
    {
        public struct Edge
        {
            public TNode Source { get; }
            public TNode Target { get; }
            public TEdgeExtraData? ExtraData { get; }
            public Edge(TNode src, TNode dst, in TEdgeExtraData? data)
            {
                Source = src;
                Target = dst;
                ExtraData = data;
            }
        }

        protected Dictionary<TNode, (List<Edge> forwardEdge, List<Edge> backwardEdge)> m_Edges = new();
        public int NodeCount => m_Edges.Count;
        public IEnumerable<Edge> GetForwardEdge(TNode node) => m_Edges[node].forwardEdge;
        public IEnumerable<Edge> GetInvEdge(TNode node) => m_Edges[node].backwardEdge;
        public void AddNode(TNode node)
        {
            if (!m_Edges.ContainsKey(node))
            {
                m_Edges.Add(node, (new(), new()));
            }
        }
        public bool ContainsNode(TNode node)
        {
            return m_Edges.ContainsKey(node);
        }
        public void AddEdge(TNode src, TNode dst, TEdgeExtraData? extra = default)
        {
            var edge = new Edge(src, dst, extra);
            if (!m_Edges.TryGetValue(src, out var srcTuple))
            {
                m_Edges.Add(src, (new() { edge }, new()));
            }
            else
            {
                srcTuple.forwardEdge.Add(edge);
            }

            if (!m_Edges.TryGetValue(dst, out var dstTuple))
            {
                m_Edges.Add(dst, (new(), new() { edge }));
            }
            else
            {
                dstTuple.backwardEdge.Add(edge);
            }

        }

        public void Dump()
        {
            foreach (var (k, v) in m_Edges)
            {
                foreach (var j in v.forwardEdge)
                {

                    Console.WriteLine($"Connect {k} to {j.Target} via {j.ExtraData}");
                }
            }
        }

        public IEnumerator<TNode> GetEnumerator()
            => ((IEnumerable<TNode>)m_Edges.Keys).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Edges.Keys.GetEnumerator();
    }
}
