using Liella.Backend.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class IndirectEmit : ICodeProcessor {
        public string Name => nameof(IndirectEmit);
        [ILCodeHandler(ILOpCode.Ldind_i4, ILOpCode.Ldind_i2, ILOpCode.Ldind_i1, ILOpCode.Ldind_u1, ILOpCode.Ldind_u2,ILOpCode.Ldind_u4)]
        public void LoadConstInt(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Int32]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldind_i8)]
        public void LoadConstInt64(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Int64]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldind_i)]
        public void LoadConstIntPtr(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.IntPtr]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldind_r4)]
        public void LoadConstFloat32(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Single]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldind_r8)]
        public void LoadConstFloat64(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Double]);
            } else {
                throw new NotImplementedException();
            }
        }
 
    }
}
