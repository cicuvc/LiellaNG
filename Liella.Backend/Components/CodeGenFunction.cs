using Liella.Backend.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public abstract class CodeGenFunction {
        public CodeGenModule Module { get; }
        public CodeGenContext Context { get; }
        public string Name { get; }
        public ICGenFunctionType FunctionType { get; }
        public abstract IReadOnlyCollection<CodeGenBasicBlock> BasicBlocks { get; }
        public abstract CodeGenBasicBlock? EntryBlock { get; }
        public abstract ReadOnlySpan<CodeGenValue> Parameters { get; }
        public CodeGenFunction(string name, ICGenFunctionType type, CodeGenModule module) {
            Name = name;
            Module = module;
            Context = module.Context;
            FunctionType = type;
        }
        public abstract CodeGenBasicBlock AddBasicBlock(string name);
        public abstract void RemoveBasicBlock(CodeGenBasicBlock del);
    }
    public abstract class CodeGenBasicBlock {
        public CodeGenFunction ParentFunction { get; }
        public CodeGenBasicBlock(CodeGenFunction parentFunction) {
            ParentFunction = parentFunction;
        }
        public abstract ICodeGenerator GetCodeGenerator();
    }
}
