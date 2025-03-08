using Liella.Backend.Types;
using LLVMSharp.Interop;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMPointerType : CGenAbstractType<LLVMPointerType, LLVMPointerTag>, ICGenType<LLVMPointerType>, ICGenPointerType, ILLVMType
    {
        protected CodeGenTypeManager m_Manager;
        public override CGenTypeTag Tag => CGenTypeTag.Pointer;

        public ICGenType ElementType => InvariantPart.ELementType;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public override int Size => m_Manager.Configuration.PointerSize;

        public override int Alignment => m_Manager.Configuration.PointerSize;

        public LLVMPointerType(in LLVMPointerTag tag, CodeGenTypeManager manager) : base(tag) {
            m_Manager = manager;
        }
        public static LLVMPointerType CreateFromKey(LLVMPointerType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart, manager);
        }
        public static LLVMPointerType Create(ICGenType elementType, int asid, CodeGenTypeManager manager)
        {
            var elementTypeRef = ((ILLVMType)elementType).InternalType;
            return CreateEntry(manager, new LLVMPointerTag(LLVMTypeRef.CreatePointer(elementTypeRef, (uint)asid), elementType));
        }
        public override string ToString() {
            if(InvariantPart.ELementType is ICGenNamedStructType namedStruct) {
                return $"{namedStruct.Name}*";
            }
            return $"{InvariantPart.ELementType}*";
        }

        public override void PrettyPrint(CGenFormattedPrinter printer, int expandLevel) {
            InvariantPart.ELementType.PrettyPrint(printer, expandLevel - 1);
            printer.Append("*");
        }
    }
}
