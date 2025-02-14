using Liella.Backend.Components;

namespace Liella.Backend.LLVM {
    public class LLVMCodeGenTargetInfo : CodeGenTargetInfo
    {
        public override string Platform { get; set; } = "pc";
        public override string ABI { get; set; } = "windows";
        public override string ObjectType { get; set; } = "elf";
        public override string TuneProcessor { get; set; } = "";
        public override string FeatureSet { get; set; } = "";
        public LLVMCodeGenTargetInfo(string arch)
        {
            Architecture = arch;
        }
    }
}
