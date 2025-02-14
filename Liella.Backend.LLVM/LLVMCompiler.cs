using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM {
    public class LLVMCompiler : CodeGenCompiler
    {
        public unsafe override CodeGenBinaryObject CompileModule(CodeGenModule module, CodeGenTargetInfo targetInfo)
        {

            var moduleRef = ((CodeGenLLVMModule)module).ModuleRef;

            var targetTriple = $"{targetInfo.Architecture}-{targetInfo.Platform}-{targetInfo.ABI}-{targetInfo.ObjectType}";
            var target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
            var machine = target.CreateTargetMachine(targetTriple, targetInfo.TuneProcessor, targetInfo.FeatureSet, LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            var errorMessage = (sbyte*)null;
            var opaqMemBuffer = (LLVMOpaqueMemoryBuffer*)null;
            LLVMSharp.Interop.LLVM.TargetMachineEmitToMemoryBuffer(machine, moduleRef, LLVMCodeGenFileType.LLVMObjectFile, &errorMessage, &opaqMemBuffer);

            var buffer = new LLVMMemoryBufferRef((nint)opaqMemBuffer);

            return new LLVMCodeGenBinaryObject(buffer);
        }

        public override void OptimizeModule(CodeGenModule module, CodeGenCompileOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
