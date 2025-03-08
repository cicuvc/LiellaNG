using Liella.Backend.Components;
using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMConstStructValue : CodeGenConstStructValue,ILLVMValue {
        protected ImmutableArray<CodeGenValue> m_Values;
        public override ReadOnlySpan<CodeGenValue> Values => m_Values.AsSpan();

        public LLVMValueRef ValueRef { get; }

        public CodeGenLLVMConstStructValue(LLVMValueRef value,ICGenType type, ImmutableArray<CodeGenValue> values) : base(type) {
            ValueRef = value;
            m_Values = values;
        }

        
    }
    public class CodeGenLLVMConstArrayValue : CodeGenConstArrayValue, ILLVMValue {
        protected ImmutableArray<CodeGenValue> m_Values;
        public CodeGenLLVMConstArrayValue(ICGenType type, LLVMValueRef value, ImmutableArray<CodeGenValue> values) : base(type) {
            ValueRef = value;
            m_Values = values;
        }

        public LLVMValueRef ValueRef { get; }

        public override ReadOnlySpan<CodeGenValue> Values => m_Values.AsSpan();
    }
    public class CodeGenLLVMBinaryValue : CodeGenLLVMValue
    {
        public BinaryOperations Operation { get; }
        public CodeGenValue LeftExpression { get; }
        public CodeGenValue RightExpression { get; }
        public NoWarpHint Hint { get; }
        public CodeGenLLVMBinaryValue(BinaryOperations op, CodeGenValue lhs, CodeGenValue rhs, LLVMValueRef value, NoWarpHint hint = NoWarpHint.Default, bool requireSameType = true) : base(lhs.Type, value)
        {
            if (requireSameType && lhs.Type != rhs.Type) throw new ArgumentException("Binary expression of different types");

            Operation = op;
            LeftExpression = lhs;
            RightExpression = rhs;
            Hint = hint;
        }
    }
}
