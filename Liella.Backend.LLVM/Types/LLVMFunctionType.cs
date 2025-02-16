using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMFunctionType : CGenAbstractType<LLVMFunctionType, LLVMFunctionTag>, ICGenFunctionType, ICGenType<LLVMFunctionType>, ILLVMType
    {
        public override CGenTypeTag Tag => CGenTypeTag.Function;

        public ICGenType ReturnType => InvariantPart.ReturnType;

        public ReadOnlySpan<ICGenType> ParamTypes => InvariantPart.ParamTypes.AsSpan();

        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public override int Size => throw new NotSupportedException();

        public override int Alignment => throw new NotSupportedException();

        public LLVMFunctionType(in LLVMFunctionTag tag) : base(tag)
        {
        }
        public static LLVMFunctionType CreateFromKey(LLVMFunctionType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMFunctionType Create(ImmutableArray<ICGenType> paramTypes, ICGenType returnType, bool isVarArgs, CodeGenTypeManager manager)
        {
            var fnType = LLVMTypeRef.CreateFunction(((ILLVMType)returnType).InternalType, paramTypes.Select(e => ((ILLVMType)e).InternalType).ToArray(), isVarArgs);
            return CreateEntry(manager, new(fnType, paramTypes, returnType, isVarArgs));
        }
        public override string ToString() {
            return InvariantPart.InternalType.PrintToString();
        }
    }
}
