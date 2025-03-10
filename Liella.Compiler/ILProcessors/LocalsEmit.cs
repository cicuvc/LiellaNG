using Liella.Backend.Compiler;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class LocalsEmit : ICodeProcessor {
        public string Name => nameof(LocalsEmit);

        public static LcTypeInfo LocalPopTypeCheck(CodeGenEvaluationContext context, LcTypeInfo onStackType, LcTypeInfo offStackType) {
            if(onStackType is LcPointerTypeInfo || onStackType is LcReferenceTypeInfo) return onStackType;
            if(onStackType is LcPrimitiveTypeInfo primType) {
                switch(primType.PrimitiveCode) {
                    case PrimitiveTypeCode.IntPtr:
                    case PrimitiveTypeCode.Int32: {
                        if(primType.PrimitiveCode == PrimitiveTypeCode.IntPtr && 
                            (offStackType is LcReferenceTypeInfo || offStackType is LcPointerTypeInfo))
                            return offStackType;
                        if(offStackType is LcPrimitiveTypeInfo {
                            PrimitiveCode: PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte
                            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                            or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                            or PrimitiveTypeCode.Char or PrimitiveTypeCode.Boolean
                            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
                        }) {
                            return offStackType;
                        } else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.Int64: {
                        if(offStackType is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 })
                            return offStackType;
                        else throw new InvalidProgramException();
                    }
                    case PrimitiveTypeCode.Double:
                    case PrimitiveTypeCode.Single: {
                        if(offStackType is LcPrimitiveTypeInfo offPrim && offPrim.PrimitiveCode == primType.PrimitiveCode)
                            return primType;
                        else throw new InvalidProgramException();
                    }
                }
            }
            // [TODO] More checks
            return offStackType;
        }
        public static LcTypeInfo LocalPushTypeCheck(CodeGenEvaluationContext context,LcTypeInfo offStackType) {
            if(offStackType is LcPointerTypeInfo || offStackType is LcReferenceTypeInfo) return offStackType;
            if(offStackType is LcPrimitiveTypeInfo primType) {
                switch(primType.PrimitiveCode) {
                    case PrimitiveTypeCode.IntPtr:
                    case PrimitiveTypeCode.UIntPtr: {
                        return context.CompileContext.PrimitiveTypes[PrimitiveTypeCode.IntPtr];
                    }
                    case PrimitiveTypeCode.Boolean:
                    case PrimitiveTypeCode.Char:
                    case PrimitiveTypeCode.Byte:
                    case PrimitiveTypeCode.SByte:
                    case PrimitiveTypeCode.Int16:
                    case PrimitiveTypeCode.UInt16:
                    case PrimitiveTypeCode.UInt32:
                    case PrimitiveTypeCode.Int32: {
                        return context.CompileContext.PrimitiveTypes[PrimitiveTypeCode.Int32];
                    }
                    case PrimitiveTypeCode.Int64:
                    case PrimitiveTypeCode.UInt64: {
                        return context.CompileContext.PrimitiveTypes[PrimitiveTypeCode.Int64];
                    }
                    case PrimitiveTypeCode.Object:
                    case PrimitiveTypeCode.String:
                    case PrimitiveTypeCode.Double:
                    case PrimitiveTypeCode.Single: {
                        return primType;
                    }
                }
            }
            // [TODO] More checks
            return offStackType;
        }


        [ILCodeHandler(ILOpCode.Ldloc, ILOpCode.Ldloc_s)]
        public void LoadLocal(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var offStackType = context.CurrentMethod.LocalVariableTypes[localVarIdx];
                var onStackType = LocalPushTypeCheck(context, offStackType);
                context.Push(onStackType);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ldloc_0, ILOpCode.Ldloc_1, ILOpCode.Ldloc_2, ILOpCode.Ldloc_3)]
        public void LoadLocalSpec(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            LoadLocal(ILOpCode.Ldloc, opcode - ILOpCode.Ldloc_0, context);
        }

        [ILCodeHandler(ILOpCode.Ldarg, ILOpCode.Ldarg_s)]
        public void LoadArgument(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var offStackType = context.CurrentMethod.ArgumentTypes[localVarIdx];
                var onStackType = LocalPushTypeCheck(context, offStackType);
                context.Push(onStackType);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ldarg_0, ILOpCode.Ldarg_1, ILOpCode.Ldarg_2, ILOpCode.Ldarg_3)]
        public void LoadArgumentSpec(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            LoadArgument(ILOpCode.Ldloc, opcode - ILOpCode.Ldarg_0, context);
        }


        [ILCodeHandler(ILOpCode.Starg, ILOpCode.Starg_s)]
        public void StoreArgument(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var onStackType = context.Pop();
                var offStackType = context.CurrentMethod.ArgumentTypes[localVarIdx];
                LocalPopTypeCheck(context, onStackType, offStackType);
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Stloc, ILOpCode.Stloc_s)]
        public void StoreLocal(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var onStackType = context.Pop();
                var offStackType = context.CurrentMethod.LocalVariableTypes[localVarIdx];
                LocalPopTypeCheck(context, onStackType, offStackType);
            } else {

            }
        }

        [ILCodeHandler(ILOpCode.Stloc_0, ILOpCode.Stloc_1, ILOpCode.Stloc_2, ILOpCode.Stloc_3)]
        public void StoreLocalSpec(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            StoreLocal(ILOpCode.Ldloc, opcode - ILOpCode.Stloc_0, context);
        }


        [ILCodeHandler(ILOpCode.Ldarga, ILOpCode.Ldarga_s)]
        public void LoadArgumentReference(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var offStackType = context.CurrentMethod.ArgumentTypes[localVarIdx];
                var referenceEntry = ReferenceTypeEntry.Create(context.CompileContext.TypeEnv.EntryManager, offStackType.Entry);
                var onStackType = context.CompileContext.NativeTypeMap[referenceEntry];
                context.Push(onStackType);
            } else {

            }
        }
        [ILCodeHandler(ILOpCode.Ldloca, ILOpCode.Ldloca_s)]
        public void LoadLocalReference(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            Debug.Assert(context.CurrentMethod is not null);

            if(context.IsTypeOnlyStage) {
                var localVarIdx = (int)operand;
                var offStackType = context.CurrentMethod.LocalVariableTypes[localVarIdx];
                var referenceEntry = ReferenceTypeEntry.Create(context.CompileContext.TypeEnv.EntryManager, offStackType.Entry);
                var onStackType = context.CompileContext.NativeTypeMap[referenceEntry];
                context.Push(onStackType);
            } else {

            }
        }
    }
}
