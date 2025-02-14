using Liella.Backend.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Types
{
    public class CodeGenTypeManager
    {
        protected HashSet<ICGenType> m_EntrySet = new();

        public T GetEntryOrAdd<T>(T key) where T : ICGenType<T>
        {
            if (!m_EntrySet.TryGetValue(key, out var actualValue))
            {
                m_EntrySet.Add(actualValue = T.CreateFromKey(key, this));
            }
            return (T)actualValue;
        }
    }
}
