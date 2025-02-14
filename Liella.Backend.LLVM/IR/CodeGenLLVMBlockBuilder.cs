using Liella.Backend.Components;
using Liella.Backend.LLVM.IR.Values;
using Liella.Backend.LLVM.Types;
using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.IR
{
    public struct CodeGenLLVMBlockBuilder : ICodeGenerator
    {
        public CodeGenLLVMBasicBlock BasicBlock { get; }
        public CodeGenLLVMBuilder Generator { get; }
        public LLVMBuilderRef Builder { get; }
        public CodeGenLLVMBlockBuilder(CodeGenLLVMBasicBlock block, CodeGenLLVMBuilder generator)
        {
            BasicBlock = block;
            Generator = generator;
            Builder = generator.Builder;
        }
        public CodeGenValue CreateAdd(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            lhs = ILLVMValue.GetLLVMValue(lhs, BasicBlock.ParentFunction.Context);
            rhs = ILLVMValue.GetLLVMValue(rhs, BasicBlock.ParentFunction.Context);

            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = hint switch
            {
                NoWarpHint.Default => Builder.BuildAdd(lValue, rValue),
                NoWarpHint.NoSignedWarp => Builder.BuildNSWAdd(lValue, rValue),
                NoWarpHint.NoUnsignedWarp => Builder.BuildNUWAdd(lValue, rValue),
                _ => throw new NotSupportedException()
            };

            return new CodeGenLLVMBinaryValue(BinaryOperations.Add, lhs, rhs, value, hint);
        }

        public CodeGenValue CreateAddrSpaceCast(CodeGenValue lhs, CodeGenValue targetAS)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildAddrSpaceCast(lValue, ((ILLVMType)targetAS).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.AsCast, lhs, value);
        }

        public CodeGenValue CreateAlloc(ICGenType type, string? name = null)
        {
            var slot = Builder.BuildAlloca(((ILLVMType)type).InternalType);

            return new CodeGenLLVMStackAlloc(BasicBlock.ParentFunction.Context.TypeFactory.CreatePointer(type), slot, (CodeGenLLVMFunction)BasicBlock.ParentFunction);
        }

        public CodeGenValue CreateAnd(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildAnd(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.And, lhs, rhs, value);
        }

        public CodeGenValue CreateArShiftRight(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildAShr(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.AShr, lhs, rhs, value);
        }

        public CodeGenValue CreateCondJump(CodeGenValue cond, CodeGenBasicBlock trueExit, CodeGenBasicBlock falseExit)
        {
            var condJump = Builder.BuildCondBr(((ILLVMValue)cond).ValueRef, ((CodeGenLLVMBasicBlock)trueExit).BlockRef, ((CodeGenLLVMBasicBlock)falseExit).BlockRef);

            return new CodeGenLLVMBranch(condJump, trueExit, cond, falseExit);
        }

        public CodeGenValue CreateDirectCast(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildBitCast(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.DirectCast, lhs, value);
        }

        public CodeGenValue CreateDiv(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;

            var value = lhs.Type.Tag.HasFlag(CGenTypeTag.Unsigned) ? Builder.BuildUDiv(lValue, rValue) : Builder.BuildSDiv(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Div, lhs, rhs, value);
        }

        public CodeGenValue CreateExtractElement(CodeGenValue lhs, CodeGenValue idx)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)idx).ValueRef;
            var value = Builder.BuildExtractElement(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Extract, lhs, idx, value);
        }

        public CodeGenValue CreateFAdd(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildFAdd(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.FAdd, lhs, rhs, value);
        }

        public CodeGenValue CreateFCast(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildFPCast(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.FCast, lhs, value);
        }

        public CodeGenValue CreateFDiv(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildFDiv(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.FDiv, lhs, rhs, value);
        }

        public CodeGenValue CreateFExt(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildFPExt(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.FExt, lhs, value);
        }

        public CodeGenValue CreateFloatCompare(CompareOp op, CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;

            var pred = op switch
            {
                CompareOp.Equal => LLVMRealPredicate.LLVMRealOEQ,
                CompareOp.NonEqual => LLVMRealPredicate.LLVMRealONE,
                CompareOp.Greater => LLVMRealPredicate.LLVMRealOGT,
                CompareOp.GreaterEqual => LLVMRealPredicate.LLVMRealOGE,
                CompareOp.Less => LLVMRealPredicate.LLVMRealOLT,
                CompareOp.LessEqual => LLVMRealPredicate.LLVMRealOLT,
                _ => throw new NotSupportedException()
            };

            var fcmp = Builder.BuildFCmp(pred, lValue, rValue);

            return new CodeGenLLVMCompare(BasicBlock.ParentFunction.Module.Context.TypeFactory.Int1, op, lhs, rhs, fcmp);
        }

        public CodeGenValue CreateFloatToSigned(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildFPToSI(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.F2Signed, lhs, value);
        }

        public CodeGenValue CreateFloatToUnsigned(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildFPToUI(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.F2Unsigned, lhs, value);
        }

        public CodeGenValue CreateFMul(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildFMul(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.FMul, lhs, rhs, value);
        }

        public CodeGenValue CreateFSub(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildFSub(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.FSub, lhs, rhs, value);
        }

        public CodeGenValue CreateFTrunc(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildFPTrunc(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.FTrunc, lhs, value);
        }

        public CodeGenValue CreateIndexing(CodeGenValue lhs, ReadOnlySpan<CodeGenValue> indices)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateIndirectJump(CodeGenValue target)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateInsertElement(CodeGenValue lhs, CodeGenValue idx, CodeGenValue value)
        {
            throw new NotImplementedException();
        }

        public CodeGenValue CreateIntCompare(CompareOp op, CodeGenValue lhs, CodeGenValue rhs)
        {
            if (!(lhs.Type.Tag.HasFlag(CGenTypeTag.Integer) && rhs.Type.Tag.HasFlag(CGenTypeTag.Integer)))
            {
                throw new ArgumentException("Integer compare requires integer operands");
            }
            if (lhs.Type.Tag.HasFlag(CGenTypeTag.Unsigned) != rhs.Type.Tag.HasFlag(CGenTypeTag.Unsigned))
            {
                throw new ArgumentException("Amiguous compare operation over unsigned and signed values");
            }
            var unsigned = lhs.Type.Tag.HasFlag(CGenTypeTag.Unsigned);

            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;

            var pred = op switch
            {
                CompareOp.Equal => LLVMIntPredicate.LLVMIntEQ,
                CompareOp.NonEqual => LLVMIntPredicate.LLVMIntNE,
                CompareOp.Greater => unsigned ? LLVMIntPredicate.LLVMIntUGT : LLVMIntPredicate.LLVMIntSGT,
                CompareOp.GreaterEqual => unsigned ? LLVMIntPredicate.LLVMIntUGE : LLVMIntPredicate.LLVMIntSGE,
                CompareOp.Less => unsigned ? LLVMIntPredicate.LLVMIntULT : LLVMIntPredicate.LLVMIntSLT,
                CompareOp.LessEqual => unsigned ? LLVMIntPredicate.LLVMIntULE : LLVMIntPredicate.LLVMIntSLE,
                _ => throw new NotSupportedException()
            };

            var icmp = Builder.BuildICmp(pred, lValue, rValue);

            return new CodeGenLLVMCompare(BasicBlock.ParentFunction.Module.Context.TypeFactory.Int1, op, lhs, rhs, icmp);
        }

        public CodeGenValue CreateIntToPtr(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildIntToPtr(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Int2Ptr, lhs, value);
        }

        public CodeGenValue CreateJump(CodeGenBasicBlock target)
        {
            var condJump = Builder.BuildBr(((CodeGenLLVMBasicBlock)target).BlockRef);

            return new CodeGenLLVMBranch(condJump, target);
        }

        public CodeGenValue CreateLoShiftRight(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildLShr(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.LShr, lhs, rhs, value);
        }

        public CodeGenValue CreateMul(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;

            var value = hint switch
            {
                NoWarpHint.Default => Builder.BuildMul(lValue, rValue),
                NoWarpHint.NoSignedWarp => Builder.BuildNSWMul(lValue, rValue),
                NoWarpHint.NoUnsignedWarp => Builder.BuildNUWMul(lValue, rValue),
                _ => throw new NotSupportedException()
            };

            return new CodeGenLLVMBinaryValue(BinaryOperations.Mul, lhs, rhs, value);
        }

        public CodeGenValue CreateNeg(CodeGenValue lhs, NoWarpHint hint = NoWarpHint.Default)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = hint switch
            {
                NoWarpHint.Default => Builder.BuildNeg(lValue),
                NoWarpHint.NoSignedWarp => Builder.BuildNSWNeg(lValue),
                NoWarpHint.NoUnsignedWarp => Builder.BuildNUWNeg(lValue),
                _ => throw new NotSupportedException()
            };

            return new CodeGenLLVMUnaryValue(UnaryOperations.Neg, lhs, value);
        }

        public CodeGenValue CreateNot(CodeGenValue lhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildNot(lValue);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Not, lhs, value);
        }

        public CodeGenValue CreateOr(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildOr(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Or, lhs, rhs, value);
        }

        public CodeGenValue CreatePtrToInt(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildPtrToInt(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Ptr2Int, lhs, value);
        }

        public CodeGenValue CreateReturn(CodeGenValue value)
        {
            var lValue = ((ILLVMValue)value).ValueRef;
            var retInst = Builder.BuildRet(lValue);
            return new CodeGenLLVMReturnValue(BasicBlock.ParentFunction.Module.Context.TypeFactory.Void, retInst);
        }

        public CodeGenValue CreateReturn()
        {
            var retInst = Builder.BuildRetVoid();
            return new CodeGenLLVMReturnValue(BasicBlock.ParentFunction.Module.Context.TypeFactory.Void, retInst);
        }

        public CodeGenValue CreateShiftLeft(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildShl(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Shl, lhs, rhs, value);
        }

        public CodeGenValue CreateSignedExt(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildSExt(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.SExt, lhs, value);
        }

        public CodeGenValue CreateSignedToFloat(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildSIToFP(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Signed2F, lhs, value);
        }

        public CodeGenValue CreateSub(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildSub(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Sub, lhs, rhs, value);
        }

        public CodeGenValue CreateTrunc(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildTrunc(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Trunc, lhs, value);
        }

        public CodeGenValue CreateUnsignedToFloat(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildUIToFP(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.Unsigned2F, lhs, value);
        }

        public CodeGenValue CreateXor(CodeGenValue lhs, CodeGenValue rhs)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var rValue = ((ILLVMValue)rhs).ValueRef;
            var value = Builder.BuildXor(lValue, rValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.Xor, lhs, rhs, value);
        }

        public CodeGenValue CreateZeroExt(CodeGenValue lhs, ICGenType type)
        {
            var lValue = ((ILLVMValue)lhs).ValueRef;
            var value = Builder.BuildZExt(lValue, ((ILLVMType)type).InternalType);

            return new CodeGenLLVMUnaryValue(UnaryOperations.ZExt, lhs, value);
        }

        public void Dispose()
        {
            Generator.Release(BasicBlock);
        }

        public CodeGenValue CreateLoad(CodeGenValue ptr)
        {
            if (ptr.Type is not LLVMPointerType ptrType) throw new ArgumentException("Load operation requires pointers");
            return CreateLoad(ptr, ptrType.ElementType);
        }

        public CodeGenValue CreateStore(CodeGenValue ptr, CodeGenValue val)
        {
            var lValue = ((ILLVMValue)ptr).ValueRef;
            var rValue = ((ILLVMValue)val).ValueRef;
            var value = Builder.BuildStore(rValue, lValue);

            return new CodeGenLLVMBinaryValue(BinaryOperations.MemStore, ptr, val, value, requireSameType: false);
        }

        public CodeGenPhiValue CreatePhi(ICGenType type)
        {
            var phi = Builder.BuildPhi(((ILLVMType)type).InternalType);

            return new CodeGenLLVMPhiValue(type, phi);
        }

        public CodeGenValue CreateLoad(CodeGenValue ptr, ICGenType type)
        {
            var lValue = ((ILLVMValue)ptr).ValueRef;
            var value = Builder.BuildLoad2(((ILLVMType)type).InternalType, lValue);

            return new CodeGenLLVMUnaryValue(UnaryOperations.MemLoad, ptr, value, type: type);
        }
    }
}
