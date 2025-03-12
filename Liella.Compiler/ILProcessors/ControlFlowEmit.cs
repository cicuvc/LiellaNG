using Liella.Backend.Compiler;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class ControlFlowEmit : ICodeProcessor {
        public string Name => nameof(ControlFlowEmit);
        [ILCodeHandler(ILOpCode.Br, ILOpCode.Br_s)]
        public void Branch(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) { } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Ret)]
        public void Return(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            var methodEntry = context.CurrentMethod!.Entry;
            if(context.IsTypeOnlyStage) {
                if(methodEntry.Signature.ReturnType is not PrimitiveTypeEntry { InvariantPart: { TypeCode: PrimitiveTypeCode.Void} }) {
                    var retType = context.Pop();
                    var offStackType = context.CurrentMethod.ResolveContextType(methodEntry.Signature.ReturnType);
                    LocalsEmit.LocalPopTypeCheck(context, retType, offStackType);
                }
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Brtrue, ILOpCode.Brtrue_s, ILOpCode.Brfalse, ILOpCode.Brfalse_s)]
        public void BranchCond(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            if(context.IsTypeOnlyStage) {
                var type = context.Pop();
                if((type is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Single or PrimitiveTypeCode.Double }) || 
                    (type is LcTypeInfo { Entry: { IsValueType: true } })) {
                    throw new InvalidProgramException("Invalid branch condition type");
                }
                
            } else {
                throw new NotImplementedException();
            }
        }
        [ILCodeHandler(ILOpCode.Call)]
        public void Call(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            // [TODO] Support varargs
            var methodEntry = context.CurrentMethod!.Entry;
            if(context.IsTypeOnlyStage) {
                var typeEnv = context.CurrentMethod.Context.TypeEnv;
                var targetMethod = typeEnv.TokenResolver.ResolveMethodToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext(), out var declType);
                var paramCount = targetMethod.Signature.ParameterTypes.Length + (methodEntry.Attributes.HasFlag(MethodAttributes.Static) ? 0 : 1);
                var paramValues = Enumerable.Range(0, paramCount).Select(e => context.Pop()).ToArray();
                if(targetMethod.Signature.ReturnType is not PrimitiveTypeEntry { InvariantPart:{TypeCode: PrimitiveTypeCode.Void } }) {
                    var returnType = context.CurrentMethod.ResolveContextType(targetMethod.Signature.ReturnType);
                    context.Push(LocalsEmit.LocalPushTypeCheck(context, returnType));
                }
                

                // [TODO] Type check
            } else {
                throw new NotImplementedException();
            }
        }


        [ILCodeHandler(ILOpCode.Callvirt)]
        public void CallVirtual(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            // [TODO] Support varargs
            var methodEntry = context.CurrentMethod!.Entry;
            if(context.IsTypeOnlyStage) {
                var typeEnv = context.CurrentMethod.Context.TypeEnv;
                var targetMethod = typeEnv.TokenResolver.ResolveMethodToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext(), out var declType);
                var paramCount = targetMethod.Signature.ParameterTypes.Length + (methodEntry.Attributes.HasFlag(MethodAttributes.Static) ? 0 : 1);
                var paramValues = Enumerable.Range(0, paramCount).Select(e => context.Pop()).ToArray();
                if(targetMethod.Signature.ReturnType is not PrimitiveTypeEntry { InvariantPart: { TypeCode: PrimitiveTypeCode.Void } }) {
                    var returnType = context.CurrentMethod.ResolveContextType(targetMethod.Signature.ReturnType);
                    context.Push(LocalsEmit.LocalPushTypeCheck(context, returnType));
                }


                // [TODO] Type check
            } else {
                throw new NotImplementedException();
            }
        }
    }
}
