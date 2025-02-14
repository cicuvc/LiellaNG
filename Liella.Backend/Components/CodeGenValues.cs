using Liella.Backend.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public class CodeGenLiteralType : ICGenType {
        private static Dictionary<CGenTypeTag, CodeGenLiteralType> m_Cache = new();
        public CGenTypeTag Tag { get; }
        private CodeGenLiteralType(CGenTypeTag tag) => Tag = tag;
        public static CodeGenLiteralType Create(CGenTypeTag tag) {
            if(!m_Cache.TryGetValue(tag, out var type)) {
                m_Cache.Add(tag, type = new CodeGenLiteralType(tag));
            }
            return type;
        }

        public bool Equals(ICGenType? other) {
            if(other is CodeGenLiteralType literal)
                return literal.Tag == Tag;
            return false;
        }
    }
    public class CodeGenLiternalValue<T> : CodeGenLiternalValue where T : unmanaged {
        public T Value { get; }
        public CodeGenLiternalValue(ICGenType type,T value) : base(type, typeof(T).Name) {
            Value = value;
        }
    }
    public class CodeGenLiternalValue : CodeGenValue {
        public string ValueType { get; }
        public CodeGenLiternalValue(ICGenType type, string valueType) : base(type) {
            ValueType = valueType;
        }
    }
    public abstract class CodeGenValue {
        public ICGenType Type { get; }
        public CodeGenValue(ICGenType type) {
            Type = type;
        }
        public static implicit operator CodeGenValue(int value) {
            return new CodeGenLiternalValue<int>(CodeGenLiteralType.Create(CGenTypeTag.Integer), value);
        }
        public static implicit operator CodeGenValue(uint value) {
            return new CodeGenLiternalValue<uint>(CodeGenLiteralType.Create(CGenTypeTag.Integer | CGenTypeTag.Unsigned), value);
        }
    }
    public abstract class CodeGenBranchValue : CodeGenValue {
        public abstract CodeGenValue? Condition { get; }
        public abstract CodeGenBasicBlock TrueExit { get; }
        public abstract CodeGenBasicBlock? FalseExit { get; }
        protected CodeGenBranchValue(ICGenType type) : base(type) {
        }
    }
    
    public abstract class CodeGenPhiValue : CodeGenValue {
        public abstract IReadOnlyCollection<(CodeGenBasicBlock incoming, CodeGenValue value)> IncomingInfo { get; }
        public abstract void AddIncomingInfo(CodeGenBasicBlock incoming, CodeGenValue value);
        protected CodeGenPhiValue(ICGenType type) : base(type) {
        }
    }
}
