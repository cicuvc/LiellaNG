using Liella.Backend.Components;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR.Values
{
    public interface ILLVMValue
    {
        LLVMValueRef ValueRef { get; }

        public static ILLVMValue GetLLVMValue(CodeGenValue value, CodeGenContext context)
        {
            if (value is ILLVMValue llvmValue) return llvmValue;
            if (value is CodeGenLiternalValue literal)
            {
                var factory = context.TypeFactory;
                switch (literal.ValueType)
                {
                    case "Int32":
                        return new CodeGenLLVMLiteral(factory.CreateIntType(32, false),
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)((CodeGenLiternalValue<int>)literal).Value));
                    case "UInt32":
                        return new CodeGenLLVMLiteral(factory.CreateIntType(32, true),
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, ((CodeGenLiternalValue<uint>)literal).Value));
                }
            }
            throw new NotSupportedException();
        }
    }
}
