using Liella.Backend.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class BitwiseEmit : ICodeProcessor {
        public string Name => nameof(BitwiseEmit);
        public static LcTypeInfo BitwiseBinaryTypeCheck(CodeGenEvaluationContext context, LcTypeInfo type1, LcTypeInfo type2) {
            if(TypeCheckHelpers.OneOf<LcPrimitiveTypeInfo>(type1, type2, out var primType, out var primOther)) {
                switch(primType!.PrimitiveCode) {
                    case PrimitiveTypeCode.Int32: {
                        if(primOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr }) {
                            return primOther;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.Int64: {
                        if(primOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int64 }) {
                            return primOther;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.IntPtr: {
                        if(primOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr }) {
                            return primType;
                        } else throw new InvalidProgramException();
                    }
                }
            }
            throw new InvalidProgramException();
        }


        public static LcTypeInfo ShiftTypeCheck(CodeGenEvaluationContext context, LcTypeInfo shiftLhs, LcTypeInfo shiftRhs) {
            if(shiftLhs is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.Int64 }) {
                if(shiftRhs is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr }) {
                    return shiftLhs;
                } 
            }
            throw new InvalidProgramException();
        }

        [ILCodeHandler(ILOpCode.Shr, ILOpCode.Shr_un)]
        public void ShiftRight(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                var shiftRhsType = context.Pop();
                var shiftLhsType = context.Pop();
                context.Push(ShiftTypeCheck(context, shiftLhsType, shiftRhsType));
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Shl)]
        public void ShiftLeft(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                var shiftRhsType = context.Pop();
                var shiftLhsType = context.Pop();
                context.Push(ShiftTypeCheck(context, shiftLhsType, shiftRhsType));
            } else {

            }
        }

        public static void BitwiseBinaryCheck(CodeGenEvaluationContext context) {
            var type1 = context.Pop();
            var type2 = context.Pop();
            context.Push(BitwiseBinaryTypeCheck(context, type1, type2));
        }

        [ILCodeHandler(ILOpCode.Xor)]
        public void BitwiseXor(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BitwiseBinaryCheck(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.And)]
        public void BitwiseAnd(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BitwiseBinaryCheck(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Or)]
        public void BitwiseOr(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BitwiseBinaryCheck(context);
            } else {

            }
        }

    }
}
