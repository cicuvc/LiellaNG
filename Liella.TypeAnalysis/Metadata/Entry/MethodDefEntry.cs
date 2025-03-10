using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Utils;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public sealed class MethodDefEntry : EntityEntryBase<MethodDefEntry, MethodDefTag, MethodDetails>, IEntityEntry<MethodDefEntry>, IMethodEntry
    {
        public override string Name => GetDetails().Name;
        public override string FullName => $"{GetDetails().DeclType.FullName}::{GetDetails().Name}";
        public override bool IsGenericInstantiation => false;
        public ITypeEntry DeclType => GetDetails().DeclType;

        public override AssemblyReaderTuple AsmInfo => InvariantPart.AsmInfo;
        public ImmutableArray<ITypeEntry> TypeArguments => GetDetails().DeclType.TypeArguments;
        public ImmutableArray<ITypeEntry> MethodArguments => GetDetails().MethodGenericParams;
        public IEnumerable<ITypeDeriveSource> DerivedType => GetDetails().DerivedEntry;
        public MethodAttributes Attributes => GetDetails().MethodDef.Attributes;
        public MethodImplAttributes ImplAttributes => GetDetails().MethodDef.ImplAttributes;
        public ILDecoder Decoder => GetDetails().ILCode;
        public MethodSignature<ITypeEntry> Signature => GetDetails().Signature;
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes => GetDetails().CustomAttributes;

        public IMethodEntry? VirtualMethodPrototype => GetDetails().VirtualMethodPrototype;

        public IEnumerable<ITypeEntry> LocalVariableTypes => GetDetails().LocalVariableTypes;

        private MethodDefEntry(TypeEnvironment typeEnv, in MethodDefTag tag) : base(typeEnv)
        {
            m_InvariantPart = tag;
        }

        public static MethodDefEntry CreateFromKey(MethodDefEntry key, TypeEnvironment typeEnv)
        {
            return new(typeEnv, key.InvariantPart);
        }
        public static MethodDefEntry Create(EntityEntryManager manager, AssemblyReaderTuple asmInfo, MethodDefinitionHandle methodDef)
        {
            return CreateEntry(manager, new(asmInfo, methodDef));
        }
        public bool IsMatchSignature(MethodSignature<ITypeEntry> signature, GenericTypeContext context)
        {
            var realSignature = GetDetails().MethodDef.DecodeSignature(TypeEnv.SignDecoder, context);

            return realSignature.RequiredParameterCount == signature.RequiredParameterCount &&
                realSignature.GenericParameterCount == signature.GenericParameterCount &&
                realSignature.ParameterTypes.SequenceEqual(signature.ParameterTypes, (u, v) => u == v) &&
                realSignature.ReturnType == signature.ReturnType;

        }
        public override void ActivateEntry(TypeCollector collector)
        {
            foreach (var i in GetDetails().DerivedEntry)
                collector.NotifyEntity(i);
        }

    }
}
