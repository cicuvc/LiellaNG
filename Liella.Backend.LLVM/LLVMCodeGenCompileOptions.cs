using Liella.Backend.Components;

namespace Liella.Backend.LLVM {
    public class LLVMCodeGenCompileOptions : CodeGenCompileOptions
    {
        public override CodeGenOptimizationLevel OptimizationLevel { get; set; }
        public override bool EanbleVerify { get; set; }
        public override bool LoopUnrolling { get; set; }
        public override bool LoopVectorization { get; set; }
        public override bool LoopInterleaving { get; set; }
        public override bool MergeFunctions { get; set; }
    }
}
