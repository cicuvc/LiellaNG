using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Namespaces;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata
{
    public class MetadataTokenResolver
    {
        public TypeEnvironment TypeEnv { get; }
        public Dictionary<PrimitiveTypeCode, TypeNode> PrimitiveTypes { get; } = new();
        public MetadataTokenResolver(TypeEnvironment typeEnv)
        {
            TypeEnv = typeEnv;
        }
        public TypeNode ResolvePrimitiveType(PrimitiveTypeCode code)
        {
            if (!PrimitiveTypes.TryGetValue(code, out var type))
            {
                if (TypeEnv.SystemLibraryTree is null)
                    throw new NullReferenceException();

                PrimitiveTypes.Add(code, type = (TypeNode)TypeEnv.SystemLibraryTree["System"][Enum.GetName(code)!, false]);
            }
            return type;
        }
        public TypeNode ResolveTypeDefinition(AssemblyReaderTuple asmInfo, TypeDefinitionHandle typeDefHandle)
        {
            var metaReader = asmInfo.MetaReader;
            var typeDef = metaReader.GetTypeDefinition(typeDefHandle);
            var typeName = metaReader.GetString(typeDef.Name);

            if (!typeDef.IsNested)
            {
                var nsName = metaReader.GetString(typeDef.Namespace);
                return TypeEnv.NamespaceTree[asmInfo.Token].FindTypeNode(nsName, typeName);
            }
            else
            {
                var parentNode = ResolveTypeDefinition(asmInfo, typeDef.GetDeclaringType());
                return (TypeNode)parentNode[typeName, false];
            }
        }
        public FieldDefEntry ResolveFieldToken(AssemblyReaderTuple asmInfo, EntityHandle handle, GenericTypeContext genericContext, out ITypeEntry exactDeclType)
        {
            var metaReader = asmInfo.MetaReader;
            switch (handle.Kind)
            {
                case HandleKind.FieldDefinition:
                    {
                        var fieldDef = metaReader.GetFieldDefinition((FieldDefinitionHandle)handle);

                        var typeEntry = ResolveTypeToken(asmInfo, fieldDef.GetDeclaringType(), genericContext);

                        exactDeclType = typeEntry;

                        return FieldDefEntry.Create(TypeEnv.EntryManager, typeEntry.AsmInfo, (FieldDefinitionHandle)handle);
                    }
                case HandleKind.MemberReference:
                    {
                        return (FieldDefEntry)ResolveMemberReference(asmInfo, (MemberReferenceHandle)handle, genericContext, out exactDeclType);
                    }
            }
            throw new NotImplementedException();
        }
        public TypeNode ResolveTypeReference(AssemblyReaderTuple asmInfo, TypeReferenceHandle typeRefHandle)
        {
            var metaReader = asmInfo.MetaReader;
            var typeRef = metaReader.GetTypeReference(typeRefHandle);
            var resolveScope = typeRef.ResolutionScope;

            var nsName = metaReader.GetString(typeRef.Namespace);
            var typeName = metaReader.GetString(typeRef.Name);

            switch (resolveScope.Kind)
            {
                case HandleKind.AssemblyReference:
                    {
                        var asmRef = metaReader.GetAssemblyReference((AssemblyReferenceHandle)resolveScope);
                        var asmToken = new AssemblyToken(metaReader, asmRef);
                        var namespaceTree = TypeEnv.NamespaceTree[asmToken];
                        return namespaceTree.FindTypeNode(nsName, typeName);
                    }
                case HandleKind.TypeReference:
                    {
                        var parentType = ResolveTypeReference(asmInfo, (TypeReferenceHandle)resolveScope);
                        return (TypeNode)parentType[typeName, false];
                    }
            }
            throw new NotImplementedException();
        }
        public ITypeEntry ResolveTypeToken(AssemblyReaderTuple asmInfo, EntityHandle handle, GenericTypeContext genericContext)
        {
            var metaReader = asmInfo.MetaReader;
            var entryManager = TypeEnv.EntryManager;
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    {
                        var typeNode = ResolveTypeDefinition(asmInfo, (TypeDefinitionHandle)handle);
                        return TypeDefEntry.Create(entryManager, typeNode.AsmInfo, typeNode.TypeDef);
                    }
                case HandleKind.TypeReference:
                    {
                        var typeNode = ResolveTypeReference(asmInfo, (TypeReferenceHandle)handle);
                        return TypeDefEntry.Create(entryManager, typeNode.AsmInfo, typeNode.TypeDef);
                    }
                case HandleKind.TypeSpecification:
                    {
                        var typeSpec = metaReader.GetTypeSpecification((TypeSpecificationHandle)handle);
                        var typeSign = typeSpec.DecodeSignature(TypeEnv.SignDecoder, genericContext);

                        return typeSign;
                    }
                default:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
        public IMethodEntry ResolveMethodToken(AssemblyReaderTuple asmInfo, EntityHandle handle, GenericTypeContext genericContext, out ITypeEntry exactDeclType)
        {
            var metaReader = asmInfo.MetaReader;
            var entryManager = TypeEnv.EntryManager;

            switch (handle.Kind)
            {
                case HandleKind.MemberReference:
                    return (IMethodEntry)ResolveMemberReference(asmInfo, (MemberReferenceHandle)handle, genericContext, out exactDeclType);
                case HandleKind.MethodDefinition:
                    {
                        var methodDef = metaReader.GetMethodDefinition((MethodDefinitionHandle)handle);
                        var typeDef = metaReader.GetTypeDefinition(methodDef.GetDeclaringType());

                        var nsName = metaReader.GetString(typeDef.Namespace);
                        var typeName = metaReader.GetString(typeDef.Name);

                        var typeNode = TypeEnv.NamespaceTree[asmInfo.Token].FindTypeNode(nsName, typeName);
                        var typeEntry = TypeDefEntry.Create(entryManager, asmInfo, typeNode.TypeDef);

                        exactDeclType = typeEntry;
                        return MethodDefEntry.Create(TypeEnv.EntryManager, typeEntry.AsmInfo, (MethodDefinitionHandle)handle);
                    }

                case HandleKind.MethodSpecification:
                    {
                        var methodSpec = metaReader.GetMethodSpecification((MethodSpecificationHandle)handle);

                        var methodGenerics = methodSpec.DecodeSignature(TypeEnv.SignDecoder, genericContext);
                        var baseMethod = ResolveMethodToken(asmInfo, methodSpec.Method, new(genericContext.TypeArguments, methodGenerics!), out exactDeclType);

                        if (baseMethod is MethodInstantiation methodInst)
                        {
                            baseMethod = (IMethodEntry)methodInst.Definition;
                        }

                        return MethodInstantiation.Create(TypeEnv.EntryManager, exactDeclType, (MethodDefEntry)baseMethod, methodGenerics!);
                    }
                default:
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }
        public IEntityEntry ResolveMemberReference(AssemblyReaderTuple asmInfo, MemberReferenceHandle handle, GenericTypeContext genericContext, out ITypeEntry exactDeclType)
        {
            var metaReader = asmInfo.MetaReader;
            var memberRef = metaReader.GetMemberReference(handle);
            var memberRefName = metaReader.GetString(memberRef.Name);
            switch (memberRef.GetKind())
            {
                case MemberReferenceKind.Field:
                    {
                        var declType = ResolveTypeToken(asmInfo, memberRef.Parent, genericContext);
                        var name = metaReader.GetString(memberRef.Name);

                        exactDeclType = declType;
                        return declType.GetField(name);
                    }
                case MemberReferenceKind.Method:
                    {
                        var declType = ResolveTypeToken(asmInfo, memberRef.Parent, genericContext);
                        var context = new GenericTypeContext(declType.TypeArguments, genericContext.MethodArguments);
                        var signature = memberRef.DecodeMethodSignature(TypeEnv.SignDecoder, context);
                        var name = metaReader.GetString(memberRef.Name);

                        var methodDef = declType.GetMethod(name, signature, context, true)!;

                        exactDeclType = declType;

                        var methodDefEntry = MethodDefEntry.Create(TypeEnv.EntryManager, declType.AsmInfo, methodDef.InvariantPart.MethodDef);
                        if (declType.IsGenericInstantiation)
                        {
                            return MethodInstantiation.Create(TypeEnv.EntryManager, exactDeclType, methodDefEntry, []);
                        }
                        return methodDefEntry;
                    }
            }

            throw new NotImplementedException();
        }
    }
}
