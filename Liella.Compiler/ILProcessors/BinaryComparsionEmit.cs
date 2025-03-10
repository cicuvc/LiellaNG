using Liella.Backend.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class BinaryComparsionEmit : ICodeProcessor {
        public string Name => nameof(BinaryComparsionEmit);

        public static LcTypeInfo BinaryComparisonTypeCheck(CodeGenEvaluationContext context,LcTypeInfo type1, LcTypeInfo type2, bool isEqCheck = false) {
            do {
                if(TypeCheckHelpers.OneOf<LcReferenceTypeInfo>(type1, type2, out var refType, out var refOther)) {
                    if(refOther is LcReferenceTypeInfo) break;
                    if(refOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr } && isEqCheck) break;
                    throw new InvalidProgramException();
                }
                if(TypeCheckHelpers.OneOf<LcPointerTypeInfo>(type1, type2, out var ptrType, out var ptrOther)) {
                    if(ptrOther is LcPointerTypeInfo) break;
                    if(ptrOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr } && isEqCheck) break;
                    throw new InvalidProgramException();
                }
                if(TypeCheckHelpers.OneOf<LcPrimitiveTypeInfo>(type1, type2, out var priType, out var priOther)) {
                    switch(priType!.PrimitiveCode) {
                        case PrimitiveTypeCode.IntPtr: {
                            if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.Int32 }) break;
                            throw new InvalidProgramException();
                        }
                        case PrimitiveTypeCode.Double: {
                            if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Double}) break;
                            throw new InvalidProgramException();
                        }
                        case PrimitiveTypeCode.Int64: {
                            if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int64 }) break;
                            throw new InvalidProgramException();
                        }
                        case PrimitiveTypeCode.Int32: {
                            if(priOther is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.Int32 }) break;
                            throw new InvalidProgramException();
                        }
                    }
                    throw new InvalidProgramException();
                }
                if(TypeCheckHelpers.OneOf<LcTypeDefInfo>(type1, type2, out var objType, out var objOther)) {
                    if(objOther is LcTypeDefInfo objOtherObj && isEqCheck) {
                        break;
                    } else throw new InvalidProgramException();
                }
            } while(false);


            return context.CompileContext.PrimitiveTypes[PrimitiveTypeCode.Int32];
        }

        public static void BinaryComparisonOpTypeHandler(CodeGenEvaluationContext context, bool isEqCheck = false) {
            var type2 = context.Pop();
            var type1 = context.Pop();
            context.Push(BinaryComparisonTypeCheck(context, type1, type2, isEqCheck));
        }

        [ILCodeHandler(ILOpCode.Beq, ILOpCode.Beq_s)]
        public void BranchEqual(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context, true);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Bne_un, ILOpCode.Bne_un_s)]
        public void BranchNonEqualUnsigned(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context, true);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ceq)]
        public void CompareEqual(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context, true);
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Cgt, ILOpCode.Cgt_un)]
        public void CompareGreater(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Clt, ILOpCode.Clt_un)]
        public void CompareLess(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Bge, ILOpCode.Bge_s)]
        public void BranchGreaterEqual(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Bge_un, ILOpCode.Bge_un_s)]
        public void BranchGreaterEqualUnsigned(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Bgt, ILOpCode.Bgt_s)]
        public void BranchGreater(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Bgt_un, ILOpCode.Bgt_un_s)]
        public void BranchGreaterUnsigned(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }


        [ILCodeHandler(ILOpCode.Blt, ILOpCode.Blt_s)]
        public void BranchLess(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Blt_un, ILOpCode.Blt_un_s)]
        public void BranchLessUnsigned(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ble, ILOpCode.Ble_s)]
        public void BranchLessEqual(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ble_un, ILOpCode.Ble_un_s)]
        public void BranchLessEqualUnsigned(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                BinaryComparisonOpTypeHandler(context);
            } else {

            }
        }
    }
}
