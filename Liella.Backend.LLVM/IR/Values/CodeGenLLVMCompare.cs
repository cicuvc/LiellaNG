using Liella.Backend.Components;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMCompare : CodeGenLLVMValue
    {
        public CompareOp Operation { get; }
        public CodeGenValue LeftExpression { get; }
        public CodeGenValue RightExpression { get; }
        public CodeGenLLVMCompare(ICGenType type, CompareOp op, CodeGenValue lhs, CodeGenValue rhs, LLVMValueRef value) : base(type, value)
        {
            Operation = op;
            LeftExpression = lhs;
            RightExpression = rhs;
        }
    }
}
