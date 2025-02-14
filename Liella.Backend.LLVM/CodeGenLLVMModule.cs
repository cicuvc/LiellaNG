using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM {
    public class CodeGenLLVMModule : CodeGenModule
    {
        public override CodeGenContext Context { get; }
        public LLVMModuleRef ModuleRef { get; protected set; }
        public CodeGenLLVMModule(string name, string target) : base(name, target)
        {
            var module = ModuleRef = LLVMModuleRef.CreateWithName(name);
            module.Target = target;

            Context = new CodeGenLLVMContext(this);
        }
        public override void DumpModule()
        {
            Console.WriteLine(ModuleRef.PrintToString());
        }
    }
}
