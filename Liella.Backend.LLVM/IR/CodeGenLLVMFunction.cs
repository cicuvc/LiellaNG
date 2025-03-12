using Liella.Backend.Components;
using Liella.Backend.LLVM.IR.Values;
using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM.IR
{
    public class CodeGenLLVMFunction : CodeGenFunction, ILLVMValue
    {
        protected ImmutableArray<CodeGenValue> m_Parameters;
        protected List<CodeGenBasicBlock> m_BasicBlocks = new();
        public LLVMValueRef LLVMFunction { get; }
        public override ReadOnlySpan<CodeGenValue> Parameters => m_Parameters.AsSpan();

        public override IReadOnlyCollection<CodeGenBasicBlock> BasicBlocks => m_BasicBlocks;

        public override CodeGenBasicBlock? EntryBlock { get; }

        public LLVMValueRef ValueRef => LLVMFunction;

        public CodeGenLLVMFunction(string name, ICGenFunctionType type, LLVMValueRef function, CodeGenLLVMModule module, bool hasImpl) : base(name, type, module)
        {
            LLVMFunction = function;

            m_Parameters = Enumerable.Range(0, (int)function.ParamsCount).Select(e =>
            {
                var value = function.GetParam((uint)e);
                return (CodeGenValue)new CodeGenLLVMFunctionParam(type.ParamTypes[e], value, this);
            }).ToImmutableArray();

            if (hasImpl)
            {
                EntryBlock = new CodeGenLLVMBasicBlock(LLVMFunction.AppendBasicBlock("entry"), this);
            }
        }

        public override CodeGenBasicBlock AddBasicBlock(string name)
        {
            var newBlock = new CodeGenLLVMBasicBlock(LLVMFunction.AppendBasicBlock("entry"), this);
            m_BasicBlocks.Add(newBlock);
            return newBlock;
        }

        public override void RemoveBasicBlock(CodeGenBasicBlock del)
        {
            throw new NotImplementedException();
        }
        public override string ToString() => Name;
    }
}
