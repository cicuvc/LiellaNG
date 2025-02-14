using Liella.Backend.LLVM.IR;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMBuilder
    {
        public LLVMBuilderRef Builder { get; }
        public CodeGenLLVMBasicBlock? CurrentBlock { get; protected set; }
        public CodeGenLLVMBuilder(LLVMBuilderRef builder)
        {
            Builder = builder;
            CurrentBlock = null;
        }
        public void SetCurrentBlock(CodeGenLLVMBasicBlock basicBlock)
        {
            if (CurrentBlock is not null)
            {
                throw new InvalidOperationException("Code generator is currently used");
            }
            CurrentBlock = basicBlock;

            Builder.PositionAtEnd(basicBlock.BlockRef);
        }
        public void Release(CodeGenLLVMBasicBlock basicBlock)
        {
            if (basicBlock != CurrentBlock)
            {
                throw new InvalidOperationException("Code generator is not used");
            }
            CurrentBlock = null;
        }


    }
}
