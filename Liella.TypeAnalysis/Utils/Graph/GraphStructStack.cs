using System.Collections;
using System.Text;

namespace Liella.TypeAnalysis.Utils.Graph
{
    public class GraphStructStack<T> where T : class
    {
        public struct GSSEnumerator : IEnumerator<T>
        {
            public Node StartNode { get; }
            public Node? CurrentNode { get; set; }
            public T Current { get; private set; }
            object IEnumerator.Current => Current;
            public GSSEnumerator(Node startNode)
            {
                CurrentNode = StartNode = startNode;
                Current = default!;
            }
            public void Dispose() { }
            public bool MoveNext()
            {
                if (CurrentNode is null) return false;
                Current = CurrentNode.Value!;
                CurrentNode = CurrentNode?.Next;
                return true;
            }

            public void Reset()
            {
                CurrentNode = StartNode;
            }
        }
        public class EmptyNode : Node, IEnumerable<T>
        {
            public EmptyNode()
            {
                m_HashCode = 114514;
                m_Length = 0;
            }
            public override IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
            public override string ToString()
            {
                return "<empty>";
            }
        }
        public class Node : IEquatable<Node>, IEnumerable<T>
        {
            protected int m_HashCode;
            protected int m_Length;
            public T? Value { get; protected set; }
            public Node? Next { get; protected set; }
            public int Length => m_Length;
            public Node() { }
            public Node(Node rhs)
            {
                Value = rhs.Value;
                Next = rhs.Next;
                m_HashCode = rhs.m_HashCode;
                m_Length = rhs.m_Length;
            }
            public Node(T value, Node next)
            {
                UpdateInfo(value, next);
            }
            public void UpdateInfo(T? value, Node? next)
            {
                Value = value;
                Next = next;

                m_HashCode = ((next?.m_HashCode ?? 0x1) * 7 + (value?.GetHashCode() ?? 0x1)) % 19260817;
                m_Length = (next?.m_Length ?? 0) + 1;
            }
            public bool Equals(Node? other)
            {
                if (other is null) return false;
                if (Value is null) return other.Value is null;
                return Next == other.Next && Value.Equals(other.Value);
            }
            public override int GetHashCode() => m_HashCode;

            public virtual IEnumerator<T> GetEnumerator() => new GSSEnumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => new GSSEnumerator(this);
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendJoin(", ", this);
                return sb.ToString();
            }
        }
        [ThreadStatic]
        protected static Node m_DeduplicationKey = new();
        protected HashSet<Node> m_DeduplicationSet = new();
        public EmptyNode Empty { get; } = new();

        public Node Push(Node currentNode, T? value)
        {
            m_DeduplicationKey.UpdateInfo(value, currentNode);
            if (!m_DeduplicationSet.TryGetValue(m_DeduplicationKey, out var actual))
            {
                actual = new(m_DeduplicationKey);
                m_DeduplicationSet.Add(actual);
            }
            return actual;
        }
        public Node Pop(Node currentNode, out T? value)
        {
            if (currentNode is null) throw new InvalidOperationException("Stack is empty");
            value = currentNode.Value;
            return currentNode.Next!;
        }
    }
}
