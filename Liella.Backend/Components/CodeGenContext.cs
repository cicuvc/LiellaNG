using Liella.Backend.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public abstract class CodeGenModule {
        public string Name { get; }
        public string Target { get; }
        public abstract CodeGenContext Context { get; }
        public CodeGenModule(string name, string target) {
            Name = name;
            Target = target;
        }
        public abstract void DumpModule();
    }
    public abstract class CodeGenContext {
        public abstract CGenTypeFactory TypeFactory { get; }
        public abstract IConstGenerator ConstGenerator { get; }
        public CodeGenTypeManager TypeManager { get; } = new();
        protected Dictionary<string, CodeGenFunction> m_Functions  = new();
        protected Dictionary<string, ICGenStructType> m_NamedStructs = new();
        protected Dictionary<string, CodeGenValue> m_GlobalValues = new();
        public IReadOnlyDictionary<string, CodeGenFunction> Functions => m_Functions;
        public IReadOnlyDictionary<string, ICGenStructType> NamedStructs => m_NamedStructs;
        public IReadOnlyDictionary<string, CodeGenValue> GlobalValues => m_GlobalValues;
        public abstract CodeGenFunction CreateFunction(string name, ICGenFunctionType type, bool hasImpl);
        public abstract CodeGenValue CreateGlobalValue(string name, CodeGenValue value);
    }
}
