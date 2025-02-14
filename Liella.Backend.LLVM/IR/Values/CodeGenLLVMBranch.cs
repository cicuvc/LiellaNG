using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMBranch : CodeGenBranchValue, ILLVMValue
    {
        public LLVMValueRef ValueRef { get; }
        public override CodeGenValue? Condition { get; }

        public override CodeGenBasicBlock TrueExit { get; }

        public override CodeGenBasicBlock? FalseExit { get; }
        public CodeGenLLVMBranch(LLVMValueRef inst, CodeGenBasicBlock trueExit, CodeGenValue? condition = null, CodeGenBasicBlock? falseExit = null) : base(trueExit.ParentFunction.Module.Context.TypeFactory.Void)
        {
            ValueRef = inst;
            TrueExit = trueExit;

            Condition = condition;
            FalseExit = falseExit;
        }
    }
}
