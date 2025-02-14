using Liella.Backend.Components;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public abstract class CodeGenLLVMValue : CodeGenValue, ILLVMValue
    {
        public LLVMValueRef ValueRef { get; }
        protected CodeGenLLVMValue(ICGenType type, LLVMValueRef value) : base(type)
        {
            ValueRef = value;
        }
    }
}
