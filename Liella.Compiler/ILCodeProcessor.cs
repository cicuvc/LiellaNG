﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Liella.Backend.Types;
using Liella.Backend.Components;
using Liella.Compiler;

namespace Liella.Backend.Compiler {
    public class CodeGenEvaluationContext {
        public LcCompileContext CompileContext { get; }
        public LcMethodInfo? CurrentMethod { get; set; }
        public bool IsTypeOnlyStage { get; set; }
        public Stack<LcTypeInfo> TypeStack { get; } = new();
        public Stack<CodeGenValue> ValueStack { get; } = new();
        public void Push(LcTypeInfo type) => TypeStack.Push(type);
        public LcTypeInfo Pop() => TypeStack.Pop();
        public CodeGenEvaluationContext(LcCompileContext compileContext) {
            CompileContext = compileContext;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ILCodeHandlerAttribute : Attribute {
        private readonly ILOpCode[] m_ILOpCodes;
        public ILOpCode[] ILOpcodes => m_ILOpCodes;
        public ILCodeHandlerAttribute(params ILOpCode[] code) {
            m_ILOpCodes = code;
        }
    }
    public interface ICodeProcessor {
        string Name { get; }
    }
    public class ILCodeProcessor {
        protected delegate void EmitHandler(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context);
        private Dictionary<ILOpCode, EmitHandler> m_DispatchMap = new Dictionary<ILOpCode, EmitHandler>();
        public void RegisterCodeProcessor(ICodeProcessor processor) {
            var type = processor.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach(var i in methods) {
                var attribute = i.GetCustomAttribute<ILCodeHandlerAttribute>();
                if(attribute != null) {
                    var delegateValue = Delegate.CreateDelegate(typeof(EmitHandler), this, i);
                    foreach(var j in attribute.ILOpcodes) {
                        m_DispatchMap.Add(j, (EmitHandler)delegateValue);
                    }
                }
            }
        }
        public void Emit(ILOpCode code, ulong operand, CodeGenEvaluationContext context) {
            if(m_DispatchMap.ContainsKey(code)) {
                m_DispatchMap[code](code, operand, context);
            } else {
                throw new NotImplementedException($"Handler for MSIL code {code} not yet implemented");
            }
        }
    }
}
