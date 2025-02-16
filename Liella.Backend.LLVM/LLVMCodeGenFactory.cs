using Liella.Backend.Components;
using Liella.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.LLVM {
    public class LLVMCodeGenFactory : CodeGenFactory {
        private static void TargetX86Init() {
            LLVMSharp.Interop.LLVM.InitializeX86Target();
            LLVMSharp.Interop.LLVM.InitializeX86TargetMC();
            LLVMSharp.Interop.LLVM.InitializeX86TargetInfo();
            LLVMSharp.Interop.LLVM.InitializeX86AsmParser();
            LLVMSharp.Interop.LLVM.InitializeX86AsmPrinter();
        }
        private static Dictionary<string, Action> m_TargetInitCode = new() {
            { "x86", TargetX86Init },
            { "x86_64", TargetX86Init }
        };
        public override CodeGenCompileOptions CreateCompileOptions()
            => new LLVMCodeGenCompileOptions();

        public override CodeGenCompiler CreateCompiler()
            => new LLVMCompiler();

        public override CodeGenModule CreateModule(string name, string target)
            => new CodeGenLLVMModule(name, target);

        public override CodeGenTargetInfo CreateTargetInfo() {
            throw new NotImplementedException();
        }

        public override void InitTarget(string targetName) {
            if(!m_TargetInitCode.TryGetValue(targetName, out var initCode)) {
                LiLogger.Default.Error(nameof(LLVMCodeGenFactory), $"Unknown LLVM target {targetName}", null);
            }
            initCode();
        }
    }
}
