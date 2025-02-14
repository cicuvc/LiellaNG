using Liella.Backend.Components;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMUnaryValue : CodeGenLLVMValue
    {
        public UnaryOperations Operation { get; }
        public CodeGenValue LeftExpression { get; }
        public NoWarpHint Hint { get; }
        public CodeGenLLVMUnaryValue(UnaryOperations op, CodeGenValue lhs, LLVMValueRef value, NoWarpHint hint = NoWarpHint.Default, ICGenType? type = null) : base(type ?? lhs.Type, value)
        {
            Operation = op;
            LeftExpression = lhs;

            Hint = hint;
        }
    }
}
