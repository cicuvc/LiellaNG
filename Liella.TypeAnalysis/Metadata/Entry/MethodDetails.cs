using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Utils;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    public struct MethodDetails : IDetails<MethodDefEntry>
    {
        public MethodDefEntry Entry { get; private set; }
        public TypeDefEntry DeclType { get; private set; }
        public MethodDefinition MethodDef { get; private set; }
        public ILDecoder? ILCode { get; private set; }
        public string Name { get; private set; }
        public MethodSignature<ITypeEntry> Signature { get; private set; }
        public ImmutableArray<ITypeEntry> MethodGenericParams { get; private set; }
        public ImmutableArray<ITypeEntry> LocalVariableTypes { get; private set; }
        public HashSet<ITypeDeriveSource> DerivedEntry { get; private set; }
        public MethodDefEntry? VirtualMethodPrototype { get; private set; }
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes { get; private set; }
        public bool IsValid => Entry is not null;

        public void CreateDetails(MethodDefEntry entry)
        {
            MethodGenericParams = ImmutableArray<ITypeEntry>.Empty;
            LocalVariableTypes = ImmutableArray<ITypeEntry>.Empty;

            var metaReader = entry.AsmInfo.MetaReader;
            var binReader = entry.AsmInfo.BinaryReader;
            var typeEnv = entry.TypeEnv;


            Entry = entry;
            MethodDef = metaReader.GetMethodDefinition(entry.InvariantPart.MethodDef);

            DeclType = TypeDefEntry.Create(typeEnv.EntryManager, entry.AsmInfo, MethodDef.GetDeclaringType());

            Name = metaReader.GetString(MethodDef.Name);


            MethodGenericParams = MethodDef.GetGenericParameters().Select(e =>
            {
                return (ITypeEntry)GenericPlaceholderTypeEntry.Create(typeEnv.EntryManager, entry, e);
            }).ToImmutableArray();

            var genericContext = new GenericTypeContext(entry.TypeArguments, MethodGenericParams);

            if (MethodDef.RelativeVirtualAddress != 0)
            {
                var methodBody = binReader.GetMethodBody(MethodDef.RelativeVirtualAddress);
                var ilCode = methodBody.GetILContent();


                if (!methodBody.LocalSignature.IsNil)
                {
                    var localSignature = metaReader.GetStandaloneSignature(methodBody.LocalSignature);

                    LocalVariableTypes = localSignature.DecodeLocalSignature(typeEnv.SignDecoder, genericContext);
                }

                ILCode = new(ilCode);
            }


            Signature = MethodDef.DecodeSignature(typeEnv.SignDecoder, genericContext);

            DerivedEntry = [.. MethodGenericParams, .. Signature.ParameterTypes, .. LocalVariableTypes, Signature.ReturnType, entry.DeclType];

            CustomAttributes = MethodDef.GetCustomAttributes().Select(e => {
                var customAttribute = entry.AsmInfo.MetaReader.GetCustomAttribute(e);
                var attribValue = customAttribute.DecodeValue(typeEnv.SignDecoder);
                var ctor = (MethodDefEntry)typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, customAttribute.Constructor, GenericTypeContext.EmptyContext, out _);
                return (ctor, arguments: attribValue);
            }).ToImmutableArray();

            // A virtual override method
            if (MethodDef.Attributes.HasFlag(MethodAttributes.Virtual) && !MethodDef.Attributes.HasFlag(MethodAttributes.NewSlot))
            {
                var parentClass = (ITypeEntry)DeclType;

                var methodPrototype = default(MethodDefEntry);

                do
                {
                    parentClass = parentClass.BaseType;
                    methodPrototype = parentClass?.GetMethod(Name, Signature, new(parentClass.TypeArguments, MethodGenericParams));
                } while (methodPrototype is null && parentClass is not null);


                if (parentClass is null) throw new InvalidProgramException("Virtual method definition not found");

                // Handle generic virtual method
                VirtualMethodPrototype = methodPrototype;
                typeEnv.Collector.RegisterVirtualChain(entry, methodPrototype!);
                //DerivedEntry.Add(MethodInstantiation.Create(typeEnv.EntryManager, parentClass, methodPrototype!, MethodGenericParams));
            }


            if (ILCode is not null)
            {
                foreach (var (opcode, operand) in ILCode)
                {
                    var opcodeInfo = ILDecoder.OpCodeMap[opcode];
                    switch (opcodeInfo.OperandType)
                    {
                        case OperandType.InlineField:
                            {
                                var fieldToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                                var fieldEntry = typeEnv.TokenResolver.ResolveFieldToken(entry.AsmInfo, fieldToken, genericContext, out var exactDeclType);

                                DerivedEntry.Add(exactDeclType);
                                DerivedEntry.Add(fieldEntry.GetDetails().FieldType);

                                break;
                            }
                        case OperandType.InlineType:
                            {
                                var typeToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                                var refType = typeEnv.TokenResolver.ResolveTypeToken(entry.AsmInfo, typeToken, genericContext);

                                DerivedEntry.Add(refType);

                                break;
                            }
                        case OperandType.InlineMethod:
                            {
                                var methodToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                                var refMethod = typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, methodToken, genericContext, out var exactDeclType);

                                DerivedEntry.Add(exactDeclType);
                                DerivedEntry.Add(refMethod);

                                break;
                            }
                    }
                }
            }
        }
    }
}
