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
    public class AllocEmit : ICodeProcessor {
        public string Name => nameof(AllocEmit);
        [ILCodeHandler(ILOpCode.Initobj)]
        public void InitObject(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            var methodEntry = context.CurrentMethod!.Entry;
            if(context.IsTypeOnlyStage) {
                var typeEnv = context.CurrentMethod.Context.TypeEnv;
                var typeEntry = typeEnv.TokenResolver.ResolveTypeToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext());
                context.Pop();
                // [TODO] Type check
            } else {
                throw new NotImplementedException();
            }
        }

        [ILCodeHandler(ILOpCode.Newobj)]
        public void NewObject(ILOpCode opcode, ulong operand, CodeGenEvaluationContext context) {
            // [TODO] Support varargs
            var methodEntry = context.CurrentMethod!.Entry;
            if(context.IsTypeOnlyStage) {
                var typeEnv = context.CurrentMethod.Context.TypeEnv;
                var targetMethod = typeEnv.TokenResolver.ResolveMethodToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext(), out var declType);
                var paramCount = targetMethod.Signature.ParameterTypes.Length;
                var paramValues = Enumerable.Range(0, paramCount).Select(e => context.Pop()).ToArray();

                // [TODO] Test required for generic classes
                var returnType = context.CurrentMethod.ResolveContextType(targetMethod.DeclType);
                context.Push(LocalsEmit.LocalPushTypeCheck(context, returnType));


                // [TODO] Type check
            } else {
                throw new NotImplementedException();
            }
        }
    }
}
