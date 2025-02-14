using System.Collections;

namespace Liella.TypeAnalysis.Namespaces
{
    public abstract class NamespaceNodeBase : IEnumerable<NamespaceNodeBase>
    {
        protected Dictionary<(string name, bool isNamespace), NamespaceNodeBase> m_SubTypes = new();
        public string Name { get; }
        public NamespaceNodeBase? Parent { get; }
        public AssemblyReaderTuple AsmInfo { get; }
        public NamespaceNodeBase(string name, AssemblyReaderTuple asmInfo, NamespaceNodeBase? parent)
        {
            Name = name;
            AsmInfo = asmInfo;
            Parent = parent;
        }
        public virtual NamespaceNodeBase this[string name, bool isNamespace = true]
        {
            get => name.Length == 0 ? this : m_SubTypes[(name, isNamespace)];
            set
            {
                if (!m_SubTypes.ContainsKey((name, isNamespace)))
                {
                    m_SubTypes.Add((name, isNamespace), value);
                }
                else
                {
                    m_SubTypes[(name, isNamespace)] = value;
                }
            }
        }
        public virtual bool ContainsNamespace(string name) => m_SubTypes.ContainsKey((name, true));
        public virtual bool ContainsType(string name) => m_SubTypes.ContainsKey((name, false));
        public virtual bool TryGetNamespace(string name, out NamespaceNodeBase? value) => m_SubTypes.TryGetValue((name, true), out value);
        public virtual bool TryGetType(string name, out NamespaceNodeBase? value) => m_SubTypes.TryGetValue((name, false), out value);

        public IEnumerator<NamespaceNodeBase> GetEnumerator() => m_SubTypes.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
