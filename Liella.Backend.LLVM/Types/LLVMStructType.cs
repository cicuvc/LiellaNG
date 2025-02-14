using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.Types
{
    public class LLVMStructType : CGenAbstractType<LLVMStructType, LLVMStructTag>, ICGenType<LLVMStructType>, ICGenStructType, ILLVMType
    {
        public override CGenTypeTag Tag => CGenTypeTag.Struct;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public ImmutableArray<ICGenType> StructTypes => InvariantPart.StructTypes;

        public ReadOnlySpan<ICGenType> Fields => InvariantPart.StructTypes.AsSpan();

        public LLVMStructType(in LLVMStructTag tag) : base(tag) { }
        public static LLVMStructType CreateFromKey(LLVMStructType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMStructType Create(ImmutableArray<ICGenType> elemenetTypes, bool packed, CodeGenTypeManager manager)
        {
            var types = elemenetTypes.Select(e => ((ILLVMType)e).InternalType).ToArray();
            return CreateEntry(manager, new LLVMStructTag(LLVMTypeRef.CreateStruct(types, packed), elemenetTypes, packed));
        }
        public static LLVMStructType Create(LLVMTypeRef types, ImmutableArray<ICGenType> elemenetTypes, bool packed, CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new LLVMStructTag(types, elemenetTypes, packed));
        }
    }
}
