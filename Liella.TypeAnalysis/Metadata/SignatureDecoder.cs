using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System.Collections.Immutable;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata
{
    public class SignatureDecoder : ISignatureTypeProvider<ITypeEntry, GenericTypeContext>, ICustomAttributeTypeProvider<ITypeEntry>
    {
        public TypeEnvironment TypeEnv { get; }
        public SignatureDecoder(TypeEnvironment typeEnv)
        {
            TypeEnv = typeEnv;
        }
        public ITypeEntry GetArrayType(ITypeEntry elementType, ArrayShape shape)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetByReferenceType(ITypeEntry elementType)
        {
            return ReferenceTypeEntry.Create(TypeEnv.EntryManager, elementType);
        }

        public ITypeEntry GetFunctionPointerType(MethodSignature<ITypeEntry> signature)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetGenericInstantiation(ITypeEntry genericType, ImmutableArray<ITypeEntry> typeArguments)
        {
            return TypeInstantiationEntry.Create(TypeEnv.EntryManager, (TypeDefEntry)genericType, typeArguments, false);
        }

        public ITypeEntry GetGenericMethodParameter(GenericTypeContext genericContext, int index)
        {
            return genericContext.MethodArguments[index] ?? throw new NullReferenceException();
        }

        public ITypeEntry GetGenericTypeParameter(GenericTypeContext genericContext, int index)
        {
            return genericContext.TypeArguments[index] ?? throw new NullReferenceException();
        }

        public ITypeEntry GetModifiedType(ITypeEntry modifier, ITypeEntry unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetPinnedType(ITypeEntry elementType)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetPointerType(ITypeEntry elementType)
        {
            return PointerTypeEntry.Create(TypeEnv.EntryManager, elementType);
        }

        public ITypeEntry GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return PrimitiveTypeEntry.Create(TypeEnv.EntryManager, typeCode);
        }

        public ITypeEntry GetSZArrayType(ITypeEntry elementType)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return TypeDefEntry.Create(TypeEnv.EntryManager, TypeEnv.GetAsmInfo(reader), handle);
        }

        public ITypeEntry GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var prototype = TypeEnv.TokenResolver.ResolveTypeReference(TypeEnv.GetAsmInfo(reader), handle);
            return TypeDefEntry.Create(TypeEnv.EntryManager, prototype.AsmInfo, prototype.TypeDef);
        }

        public ITypeEntry GetTypeFromSpecification(MetadataReader reader, GenericTypeContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        public ITypeEntry GetSystemType() {
            throw new NotImplementedException();
        }

        public ITypeEntry GetTypeFromSerializedName(string name) {
            throw new NotImplementedException();
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(ITypeEntry type) {
            var fieldType = (PrimitiveTypeEntry)type.GetField("value__").FieldType;
            return fieldType.InvariantPart.TypeCode;
        }

        public bool IsSystemType(ITypeEntry type) {
            return (type.FullName == ".System.Type@0");
        }
    }
}
