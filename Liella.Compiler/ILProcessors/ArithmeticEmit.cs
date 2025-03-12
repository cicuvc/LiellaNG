using Liella.Backend.Compiler;
using Liella.Backend.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class ArithmeticEmit : ICodeProcessor {
        public string Name => nameof(ArithmeticEmit);
        public static LcTypeInfo BinaryOpTypeCheck(LcTypeInfo type1, LcTypeInfo type2) {
            if(TypeCheckHelpers.OneOf<LcPointerTypeInfo>(type1, type2, out var ptrType, out var ptrOther)) {
                if((ptrOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr })) {
                    return ptrType!;
                } else throw new InvalidProgramException();
            }
            if(TypeCheckHelpers.OneOf<LcReferenceTypeInfo>(type1, type2, out var refType, out var refOther)) {
                if((ptrOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr })) {
                    return ptrType!;
                } else throw new InvalidProgramException();
            }
            if(TypeCheckHelpers.OneOf<LcPrimitiveTypeInfo>(type1, type2, out var primType, out var priOther)) {
                switch(primType!.PrimitiveCode) {
                    case PrimitiveTypeCode.Single:
                    case PrimitiveTypeCode.Double: {
                        if(priOther is LcPrimitiveTypeInfo priOtherPri && priOtherPri.PrimitiveCode == primType!.PrimitiveCode) {
                            return primType;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.IntPtr: {
                        if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.Int32}) {
                            return primType;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.Int64: {
                        if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int64 }) {
                            return primType;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.Int32: {
                        if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.Int32 }) {
                            return priOther;
                        } else throw new InvalidProgramException();
                    }
                }
            }
            throw new InvalidProgramException();
        }
        public static void BinaryArithmetricOpTypeHandler(CodeGenEvaluationContext context) {
            var type2 = context.Pop();
            var type1 = context.Pop();
            context.Push(BinaryOpTypeCheck(type1, type2));
        }

        [ILCodeHandler(ILOpCode.Add,ILOpCode.Add_ovf, ILOpCode.Add_ovf_un)]
        public void ArithmetricAdd(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryArithmetricOpTypeHandler(context);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Nop)]
        public void Nop(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {

        }
        [ILCodeHandler(ILOpCode.Sub,ILOpCode.Sub_ovf, ILOpCode.Sub_ovf_un)]
        public void ArithmetricSub(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryArithmetricOpTypeHandler(context);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Mul, ILOpCode.Mul_ovf, ILOpCode.Mul_ovf_un)]
        public void ArithmetricMul(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryArithmetricOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Div, ILOpCode.Div_un)]
        public void ArithmetricDiv(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryArithmetricOpTypeHandler(context);
            } else {
                throw new NotImplementedException();
            }
        }

        [ILCodeHandler(ILOpCode.Rem, ILOpCode.Rem_un)]
        public void ArithmetricRem(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryArithmetricOpTypeHandler(context);
            } else {
                throw new NotImplementedException();
            }
        }


        [ILCodeHandler(ILOpCode.Ldc_r4)]
        public void LoadConstFloat32(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Single]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldc_r8)]
        public void LoadConstFloat64(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Double]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldc_i8)]
        public void LoadConstInt64(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Int64]);
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ldc_i4, ILOpCode.Ldc_i4_s)]
        public void LoadConstInt32(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                context.Push(context.CurrentMethod!.Context.PrimitiveTypes[PrimitiveTypeCode.Int32]);
            } else {
                throw new NotImplementedException();
            }
        }

        [ILCodeHandler(ILOpCode.Ldc_i4_0, ILOpCode.Ldc_i4_1, ILOpCode.Ldc_i4_2, ILOpCode.Ldc_i4_3, ILOpCode.Ldc_i4_4, ILOpCode.Ldc_i4_5, ILOpCode.Ldc_i4_6, ILOpCode.Ldc_i4_7, ILOpCode.Ldc_i4_8, ILOpCode.Ldc_i4_m1)]
        public void LoadConstInt32Spec(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            LoadConstInt32(opcode, (ulong)(opcode - ILOpCode.Ldc_i4_0), context);
        }
    }
}
