using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public enum CodeGenRelocationMode {
        NoReloc, PIC
    }
    public abstract class CodeGenTargetInfo {
        public string Architecture { get; set; } = "";
        public abstract string Platform { get; set; }
        public abstract string ABI { get; set; }
        public abstract string ObjectType { get; set; }

        public abstract string TuneProcessor { get; set; }
        public abstract string FeatureSet { get; set; }
    }
    public enum CodeGenOptimizationLevel {
        O0, O1, O2, O3, Os, Custom
    }
    public abstract class CodeGenCompileOptions {
        public abstract CodeGenOptimizationLevel OptimizationLevel { get; set; }
        public abstract bool EanbleVerify { get; set; }
        public abstract bool LoopUnrolling { get; set; }
        public abstract bool LoopVectorization { get; set; }
        public abstract bool LoopInterleaving { get; set; }
        public abstract bool MergeFunctions { get; set; }
    }
    public abstract class CodeGenBinaryObject {
        public abstract ReadOnlySpan<byte> ObjectBuffer { get; }
    }
    public abstract class CodeGenCompiler {
        public abstract void OptimizeModule(CodeGenModule module, CodeGenCompileOptions options);
        public abstract CodeGenBinaryObject CompileModule(CodeGenModule module, CodeGenTargetInfo targetInfo);
    }
    public abstract class CodeGenLinker {

    }
}
