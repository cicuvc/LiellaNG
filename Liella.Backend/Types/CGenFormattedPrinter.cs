using System.Diagnostics;
using System.Text;

namespace Liella.Backend.Types {
    public class CGenFormattedPrinter {
        protected StringBuilder m_Buffer = new();
        protected int m_IndentCount;
        protected int m_Indent;
        protected char m_IndentChar;
        protected string m_IndentPrefix = "";
        protected bool m_CurrentLineEmpty = true;

        public bool CurrentLineEmpty => m_CurrentLineEmpty;
        private struct FormattedIndentContext : IDisposable {
            private CGenFormattedPrinter m_Context;
            public FormattedIndentContext(CGenFormattedPrinter context) => m_Context = context;
            public void Dispose() => m_Context.EndIndent();
        }
        public CGenFormattedPrinter(int indentCount = 4, char indentChar = ' ') {
            m_IndentCount = indentCount;
            m_IndentChar = indentChar;
        }
        public IDisposable BeginIndent() {
            m_Indent += m_IndentCount;
            m_IndentPrefix = new(m_IndentChar, m_Indent);
            return new FormattedIndentContext(this);
        }
        public void EndIndent() {
            m_Indent -= m_IndentCount;
            m_IndentPrefix = new(m_IndentChar, m_Indent);
        }
        public string Dump() {
            return m_Buffer.ToString();
        }
        public void Append(string s) {
            Debug.Assert(!s.Contains('\r'));
            Debug.Assert(!s.Contains('\n'));
            if(m_CurrentLineEmpty) {
                m_CurrentLineEmpty = false;
                m_Buffer.Append(m_IndentPrefix);
            }
            m_Buffer.Append(s);
        }
        public void AppendLine(string s = "") {
            Debug.Assert(!s.Contains('\r'));
            Debug.Assert(!s.Contains('\n'));
            if(m_CurrentLineEmpty) {
                m_CurrentLineEmpty = false;
                m_Buffer.Append(m_IndentPrefix);
            }
            m_Buffer.Append(s);
            m_Buffer.Append(Environment.NewLine);
            m_CurrentLineEmpty = true;
        }
        public void AppendFormat(string fmt, params object[] values)
            => Append(string.Format(fmt, values));
    }
}
