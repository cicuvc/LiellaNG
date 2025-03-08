using Liella.Backend.Components;
using Liella.Backend.LLVM.IR;
using Liella.Backend.LLVM.IR.Values;
using Liella.Backend.LLVM.Types;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM
{
    public class CodeGenLLVMContext : CodeGenContext
    {
        public LLVMModuleRef ModuleRef { get; }
        public LLVMContextRef ContextRef { get; }
        public CodeGenLLVMModule Module { get; }
        public override CGenTypeFactory TypeFactory { get; }
        protected ThreadLocal<CodeGenLLVMBuilder> m_Builders;
        public override IConstGenerator ConstGenerator { get; }

        public override CodeGenFunction CreateFunction(string name, ICGenFunctionType type, bool hasImpl)
        {
            var function = ModuleRef.AddFunction(name, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMFunction(name, type, function, Module, hasImpl);
        }

        public override CodeGenGlobalPtrValue CreateGlobalValue(string name, ICGenType elementType, CodeGenValue? value)
        {
            var valuePtr = ModuleRef.AddGlobal(((ILLVMType)elementType).InternalType, name);
            if(value is not null) {
                if(value.Type != elementType) {
                    throw new ArgumentException("Type mismatch");
                }
                valuePtr.Initializer = ((ILLVMValue)value).ValueRef;
            }
            var ptrType = TypeFactory.CreatePointer(elementType);
            return new CodeGenLLVMGloPtr(valuePtr, ptrType, elementType, value);
        }
        public CodeGenLLVMContext(CodeGenLLVMModule llvmModule)
        {
            Module = llvmModule;
            ModuleRef = llvmModule.ModuleRef;
            ContextRef = llvmModule.ModuleRef.Context;
            TypeFactory = new LLVMTypeFactory(this);
            ConstGenerator = new CodeGenLLVMConstGenerator(this);

            m_Builders = new ThreadLocal<CodeGenLLVMBuilder>();
        }
        public CodeGenLLVMBuilder GetCurrentBuilder()
        {
            if (m_Builders.Value is null)
            {
                m_Builders.Value = new(ContextRef.CreateBuilder());
            }
            return m_Builders.Value;
        }
    }
}
