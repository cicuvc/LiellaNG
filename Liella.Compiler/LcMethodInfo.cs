using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public enum LcMethodInitStage {
        PendingFunctionType = 0x1,
        CompleteFunctionType = 0x2
    }
    public class LcMethodInfo {
        public  LcCompileContext Context { get; }
        public CodeGenContext CgContext { get; }
        public LcTypeInfo DeclType { get; }
        public IMethodEntry Entry { get; }
        public bool IsStatic { get; }
        public bool IsVirtualOverride { get; }
        public bool IsVirtualDef { get; }
        public bool HasBody { get; }
        public ILMethodAnalyzer? ILCodeAnalyzer { get; }
        protected ICGenFunctionType? m_MethodType;
        protected CodeGenFunction? m_MethodFunction;
        public LcMethodInitStage InitState { get; protected set; }
        public LcMethodInfo(LcTypeInfo type, IMethodEntry entry,LcCompileContext context, CodeGenContext cgContext) {
            Context = context;
            CgContext = cgContext;

            DeclType = type;
            Entry = entry;

            var methodDef = entry is MethodDefEntry defEntry ? defEntry : (MethodDefEntry)((MethodInstantiation)entry).Definition;

            IsStatic = methodDef.Attriutes.HasFlag(MethodAttributes.Static);
            IsVirtualOverride = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && !methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);
            IsVirtualDef = methodDef.Attriutes.HasFlag(MethodAttributes.Virtual) && methodDef.Attriutes.HasFlag(MethodAttributes.NewSlot);

            HasBody = !Entry.Attributes.HasFlag(MethodAttributes.Abstract); 

            if(HasBody)
                ILCodeAnalyzer = new(entry.Decoder);

            
        }
        protected bool CheckTypeInitialized(LcMethodInitStage pending, LcMethodInitStage complete) {
            if(InitState.HasFlag(complete)) return true;
            if(InitState.HasFlag(pending)) {
                throw new InvalidOperationException("Bad function init cause infinity recursive");
            }
            InitState ^= pending;
            return false;
        }
        protected void SetTypeInitialized(LcMethodInitStage pending, LcMethodInitStage complete) {
            InitState ^= pending ^ complete;
        }
        protected virtual void InitializeFunction() {

            var argumentsType = Entry.Signature.ParameterTypes.Select(e => ResolveContextType(e).GetInstanceTypeEnsureDef()).ToImmutableArray();
            var returnType = ResolveContextType(Entry.Signature.ReturnType).GetInstanceTypeEnsureDef();

            m_MethodType = CgContext.TypeFactory.CreateFunction(argumentsType.AsSpan(), returnType);

            // pure virtual function or normal function
            if(HasBody)
                m_MethodFunction = CgContext.CreateFunction(Entry.FullName, m_MethodType, HasBody);
        }
        public ICGenFunctionType GetMethodTypeEnsureDef() {
            if(!CheckTypeInitialized(LcMethodInitStage.PendingFunctionType, LcMethodInitStage.CompleteFunctionType)) {
                InitializeFunction();
            }
            SetTypeInitialized(LcMethodInitStage.PendingFunctionType, LcMethodInitStage.CompleteFunctionType);
            return m_MethodType!;
        }
        public CodeGenFunction GetMethodValueEnsureDef() {
            if(!HasBody) throw new NotSupportedException();
            if(!CheckTypeInitialized(LcMethodInitStage.PendingFunctionType, LcMethodInitStage.CompleteFunctionType)) {
                InitializeFunction();
            }
            SetTypeInitialized(LcMethodInitStage.PendingFunctionType, LcMethodInitStage.CompleteFunctionType);
            return m_MethodFunction!;
        }
        protected virtual LcTypeInfo ResolveContextType(ITypeEntry entry) {
            return DeclType.ResolveContextType(entry)!;
        }
        public void GenerateDecl() {
            
        }
        public void GenerateCode() {

        }
    }
}
