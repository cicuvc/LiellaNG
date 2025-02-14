using Liella.Backend.Components;
using Liella.Backend.LLVM.IR.Values;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR
{
    public class CodeGenLLVMConstGenerator : IConstGenerator
    {
        public CodeGenValue CreateAdd(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            var value = LLVMValueRef.CreateConstAdd(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.Add, lhs, rhs, value, hint);
        }

        public CodeGenValue CreateAddrSpaceCast(CodeGenValue lhs, CodeGenValue targetAS)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateAnd(CodeGenValue lhs, CodeGenValue rhs)
        {
            var value = LLVMValueRef.CreateConstAnd(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.And, lhs, rhs, value);
        }

        public CodeGenValue CreateArShiftRight(CodeGenValue lhs, CodeGenValue rhs)
        {
            var value = LLVMValueRef.CreateConstAShr(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.AShr, lhs, rhs, value);
        }

        public CodeGenValue CreateConstString(string value, bool nullEnd = true)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateDirectCast(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateDiv(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateExtractElement(CodeGenValue lhs, CodeGenValue idx)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFAdd(CodeGenValue lhs, CodeGenValue rhs)
        {
            var value = LLVMValueRef.CreateConstAdd(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.Add, lhs, rhs, value);
        }

        public CodeGenValue CreateFCast(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFDiv(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotSupportedException();
        }

        public CodeGenValue CreateFExt(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFloatToSigned(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFloatToUnsigned(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFMul(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFSub(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateFTrunc(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateIndexing(CodeGenValue lhs, ReadOnlySpan<CodeGenValue> indices)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateInsertElement(CodeGenValue lhs, CodeGenValue idx, CodeGenValue value)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateIntToPtr(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateLoShiftRight(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateMul(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateNeg(CodeGenValue lhs, NoWarpHint hint = NoWarpHint.Default)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateNot(CodeGenValue lhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateOr(CodeGenValue lhs, CodeGenValue rhs)
        {
            var value = LLVMValueRef.CreateConstOr(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.Or, lhs, rhs, value);
        }

        public CodeGenValue CreatePtrToInt(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateShiftLeft(CodeGenValue lhs, CodeGenValue rhs)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateSignedExt(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateSignedToFloat(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateSub(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            var value = hint switch
            {
                NoWarpHint.Default => LLVMValueRef.CreateConstSub(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef),
                NoWarpHint.NoSignedWarp => LLVMValueRef.CreateConstNSWSub(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef),
                NoWarpHint.NoUnsignedWarp => LLVMValueRef.CreateConstNUWSub(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef),
                _ => throw new NotImplementedException()
            };
            return new CodeGenLLVMBinaryValue(BinaryOperations.Or, lhs, rhs, value);
        }

        public CodeGenValue CreateTrunc(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateUnsignedToFloat(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateXor(CodeGenValue lhs, CodeGenValue rhs)
        {
            var value = LLVMValueRef.CreateConstXor(((ILLVMValue)lhs).ValueRef, ((ILLVMValue)rhs).ValueRef);
            return new CodeGenLLVMBinaryValue(BinaryOperations.Xor, lhs, rhs, value);
        }

        public CodeGenValue CreateZeroExt(CodeGenValue lhs, ICGenType type)
        {
            throw new NotImplementedException();
        }
    }
}
