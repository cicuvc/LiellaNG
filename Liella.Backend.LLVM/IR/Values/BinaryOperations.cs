namespace Liella.Backend.LLVM.IR.Values
{
    public enum BinaryOperations
    {
        Add, Sub, Mul, Div, And, Or, Xor, Rem,
        AShr, LShr, Shl,

        FAdd, FSub, FMul, FDiv, FRem,

        Extract, MemStore
    }
}
