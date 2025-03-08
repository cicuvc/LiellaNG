using Liella.Backend.Components;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public class CodeGenLLVMGloPtr : CodeGenGlobalPtrValue,ILLVMValue
    {
        protected CodeGenValue? m_Initializer;
        public CodeGenLLVMGloPtr(LLVMValueRef valueRef, ICGenType type, ICGenType elementType, CodeGenValue? initializer) : base(type, elementType)
        {
            m_Initializer = initializer;
            ValueRef = valueRef;
        }

        public override CodeGenValue? Initializer {
            get => m_Initializer;
            set {
                m_Initializer = value;

                var llvmPtr = ValueRef;
                llvmPtr.Initializer = ((ILLVMValue)value!).ValueRef;
            }
        }

        public LLVMValueRef ValueRef { get; }
    }
}
