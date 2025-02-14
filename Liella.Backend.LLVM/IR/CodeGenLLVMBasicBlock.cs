using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR
{
    public class CodeGenLLVMBasicBlock : CodeGenBasicBlock {
        public LLVMBasicBlockRef BlockRef { get; }
        public LLVMValueRef CurrentLocation { get; }
        public CodeGenLLVMBasicBlock(LLVMBasicBlockRef blockRef, CodeGenLLVMFunction function) : base(function) {
            BlockRef = blockRef;
        }

        public override ICodeGenerator GetCodeGenerator() {
            var module = (CodeGenLLVMModule)((CodeGenLLVMFunction)ParentFunction).Module;
            var context = (CodeGenLLVMContext)module.Context;
            var generatorRef = context.GetCurrentBuilder();
            var generator = new CodeGenLLVMBlockBuilder(this, generatorRef);

            generatorRef.SetCurrentBlock(this);

            return generator;
        }
    }
}
