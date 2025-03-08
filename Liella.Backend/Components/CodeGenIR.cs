using Liella.Backend.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public enum NoWarpHint {
        Default, NoSignedWarp, NoUnsignedWarp
    }
    public enum CompareOp {
        Equal, NonEqual,
        Greater, Less,
        GreaterEqual, LessEqual
    }
    public interface IValueOperations {
        CodeGenValue CreateAdd(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default);
        CodeGenValue CreateSub(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default);
        CodeGenValue CreateMul(CodeGenValue lhs, CodeGenValue rhs, NoWarpHint hint = NoWarpHint.Default);
        CodeGenValue CreateNeg(CodeGenValue lhs, NoWarpHint hint = NoWarpHint.Default);
        CodeGenValue CreateDiv(CodeGenValue lhs, CodeGenValue rhs);

        CodeGenValue CreateAnd(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateXor(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateOr(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateNot(CodeGenValue lhs);

        CodeGenValue CreateArShiftRight(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateLoShiftRight(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateShiftLeft(CodeGenValue lhs, CodeGenValue rhs);

        CodeGenValue CreateTrunc(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateDirectCast(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateSignedExt(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateZeroExt(CodeGenValue lhs, ICGenType type);

        CodeGenValue CreateIntToPtr(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreatePtrToInt(CodeGenValue lhs, ICGenType type);

        CodeGenValue CreateFAdd(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateFSub(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateFMul(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateFDiv(CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateFTrunc(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateFCast(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateFExt(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateFloatToUnsigned(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateFloatToSigned(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateUnsignedToFloat(CodeGenValue lhs, ICGenType type);
        CodeGenValue CreateSignedToFloat(CodeGenValue lhs, ICGenType type);

        CodeGenValue CreateAddrSpaceCast(CodeGenValue lhs, CodeGenValue targetAS);

        CodeGenValue CreateInsertElement(CodeGenValue lhs, CodeGenValue idx, CodeGenValue value);
        CodeGenValue CreateExtractElement(CodeGenValue lhs, CodeGenValue idx);

        CodeGenValue CreateIndexing(CodeGenValue lhs, ReadOnlySpan<CodeGenValue> indices);

    }
    public interface ISequentialOperations {
        CodeGenValue CreateAlloc(ICGenType type, string? name = null);
        CodeGenValue CreateLoad(CodeGenValue ptr);
        CodeGenValue CreateLoad(CodeGenValue ptr, ICGenType type);
        CodeGenValue CreateStore(CodeGenValue ptr, CodeGenValue value);
        CodeGenValue CreateIntCompare(CompareOp op, CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateFloatCompare(CompareOp op, CodeGenValue lhs, CodeGenValue rhs);
        CodeGenValue CreateIndirectJump(CodeGenValue target);
        CodeGenValue CreateJump(CodeGenBasicBlock target);
        CodeGenValue CreateCondJump(CodeGenValue cond, CodeGenBasicBlock trueExit, CodeGenBasicBlock falseExit);
        CodeGenValue CreateReturn(CodeGenValue value);
        CodeGenValue CreateReturn();
        CodeGenPhiValue CreatePhi(ICGenType type);
    }

    public interface ICodeGenerator: IValueOperations, ISequentialOperations, IDisposable {

    }
    public interface IConstGenerator: IValueOperations {
        CodeGenValue CreateConstString(string value, bool nullEnd = true);
        CodeGenConstStructValue CreateConstStruct(ICGenStructType type, ReadOnlySpan<CodeGenValue> types);
        CodeGenConstArrayValue CreateConstArray(ICGenArrayType type, ReadOnlySpan<CodeGenValue> values);
    }
}
