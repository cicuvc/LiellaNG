using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
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
