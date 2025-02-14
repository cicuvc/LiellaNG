using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMUnaryIdxValue : CodeGenLLVMValue
    {
        public UnaryOperations Operation { get; }
        public CodeGenValue LeftExpression { get; }
        public NoWarpHint Hint { get; }
        public CodeGenLLVMUnaryIdxValue(UnaryOperations op, CodeGenValue lhs, LLVMValueRef value, NoWarpHint hint = NoWarpHint.Default) : base(lhs.Type, value)
        {
            Operation = op;
            LeftExpression = lhs;

            Hint = hint;
        }
    }
}
