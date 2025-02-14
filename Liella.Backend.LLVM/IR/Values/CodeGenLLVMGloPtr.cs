using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMGloPtr : CodeGenLLVMValue
    {
        public CodeGenLLVMGloPtr(LLVMValueRef valueRef, ICGenType type) : base(type, valueRef)
        {
        }
    }
}
