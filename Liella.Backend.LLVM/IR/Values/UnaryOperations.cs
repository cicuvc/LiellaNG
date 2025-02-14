namespace Liella.Backend.LLVM.IR.Values
{
    public enum UnaryOperations
    {
        Neg, Not, Trunc, ZExt, SExt, DirectCast,
        FCast, FExt, FTrunc,
        F2Signed, F2Unsigned, Signed2F, Unsigned2F,
        Int2Ptr, Ptr2Int,
        AsCast, MemLoad
    }
}
