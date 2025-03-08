using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMArrayType : CGenAbstractType<LLVMArrayType, LLVMArrayTag>, ICGenType<LLVMArrayType>, ICGenArrayType, ILLVMType
    {
        public override CGenTypeTag Tag => CGenTypeTag.Array;

        public ICGenType ElementType => InvariantPart.ELementType;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public int Length => InvariantPart.Length;

        public override int Size => ElementType.Size * Length;

        public override int Alignment => ElementType.Alignment;

        public LLVMArrayType(in LLVMArrayTag tag) : base(tag) { }
        public static LLVMArrayType CreateFromKey(LLVMArrayType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMArrayType Create(ICGenType elementType, int length, CodeGenTypeManager manager)
        {
            var elementTypeRef = ((ILLVMType)elementType).InternalType;
            return CreateEntry(manager, new LLVMArrayTag(LLVMTypeRef.CreateArray(elementTypeRef, (uint)length), elementType, length));
        }
        public override string ToString() {
            return InvariantPart.InternalType.PrintToString();
        }

        public override void PrettyPrint(CGenFormattedPrinter printer, int expandLevel) {
            ElementType.PrettyPrint(printer, expandLevel - 1);
            printer.Append($"[{InvariantPart.Length}]");
        }
    }
}
