using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMStackAlloc : CodeGenLLVMValue
    {
        public CodeGenLLVMFunction ParentFunction { get; }
        public CodeGenLLVMStackAlloc(ICGenType type, LLVMValueRef value, CodeGenLLVMFunction function) : base(type, value)
        {
            ParentFunction = function;
        }
    }
}
