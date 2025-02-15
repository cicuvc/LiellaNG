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

        public int Size { get; }

        public int Alignment { get; }


        public CodeGenLiteralType(CGenTypeTag tag, int size, int alignment) {
            Size = size;
            Alignment = alignment;
            Tag = tag;
        }

        public bool Equals(ICGenType? other) {
            if(other is CodeGenLiteralType literal)
                return literal.Tag == Tag && literal.Size == Size && literal.Alignment == Alignment;
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
            return new CodeGenLiternalValue<int>(new CodeGenLiteralType(CGenTypeTag.Integer, 4, 4), value);
        }
        public static implicit operator CodeGenValue(uint value) {
            return new CodeGenLiternalValue<uint>(new CodeGenLiteralType(CGenTypeTag.Integer | CGenTypeTag.Unsigned, 4,4), value);
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
