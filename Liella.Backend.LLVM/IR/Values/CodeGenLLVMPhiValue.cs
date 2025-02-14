using Liella.Backend.Components;
using Liella.Backend.LLVM.IR;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMPhiValue : CodeGenPhiValue, ILLVMValue
    {
        protected List<(CodeGenBasicBlock incoming, CodeGenValue value)> m_Incomings = new();
        public CodeGenLLVMPhiValue(ICGenType type, LLVMValueRef valueRef) : base(type)
        {
            ValueRef = valueRef;
        }

        public LLVMValueRef ValueRef { get; }

        public override IReadOnlyCollection<(CodeGenBasicBlock incoming, CodeGenValue value)> IncomingInfo => m_Incomings;

        public override void AddIncomingInfo(CodeGenBasicBlock incoming, CodeGenValue value)
        {
            m_Incomings.Add((incoming, value));
            ValueRef.AddIncoming([((ILLVMValue)value).ValueRef], [((CodeGenLLVMBasicBlock)incoming).BlockRef], 1);
        }
    }
}
