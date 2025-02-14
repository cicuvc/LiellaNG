using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMFunctionParam : CodeGenLLVMValue
    {
        public CodeGenLLVMFunction ParentFunction { get; }
        public CodeGenLLVMFunctionParam(ICGenType type, LLVMValueRef value, CodeGenLLVMFunction func) : base(type, value)
        {
            ParentFunction = func;
        }
    }
}
