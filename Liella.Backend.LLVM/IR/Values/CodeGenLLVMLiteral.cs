using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMLiteral : CodeGenLLVMValue
    {
        public CodeGenLLVMLiteral(ICGenType type, LLVMValueRef value) : base(type, value)
        {
        }
    }
}
