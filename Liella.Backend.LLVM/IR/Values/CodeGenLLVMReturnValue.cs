using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMReturnValue : CodeGenLLVMValue
    {
        public CodeGenLLVMReturnValue(ICGenType type, LLVMValueRef value) : base(type, value)
        {
        }
    }
}
