using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.Types
{
    public struct LLVMFunctionTag : IEquatable<LLVMFunctionTag>
    {
        public LLVMTypeRef InternalType { get; }
        public ImmutableArray<ICGenType> ParamTypes { get; }
        public ICGenType ReturnType { get; }
        public bool IsVarArgs { get; }
        public LLVMFunctionTag(LLVMTypeRef internalType, ImmutableArray<ICGenType> paramTypes, ICGenType returnType, bool isVarArgs)
        {
            InternalType = internalType;
            ParamTypes = paramTypes;
            ReturnType = returnType;
            IsVarArgs = isVarArgs;
        }
        public override int GetHashCode()
        {
            return InternalType.Handle.GetHashCode() ^ ReturnType.GetHashCode() ^ ParamTypes.GetHashCode();
        }
        public bool Equals(LLVMFunctionTag other)
        {
            return InternalType == other.InternalType && ReturnType == other.ReturnType && ParamTypes.SequenceEqual(other.ParamTypes);
        }
    }
}
