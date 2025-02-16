using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Namespaces;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    
    public struct TypeDefDetails : IDetails<TypeDefEntry>
    {
        public TypeAttributes Attributes { get; private set; }
        public bool IsValueType { get; private set; }
        public bool IsEnum { get; private set; }
        public string Name { get; private set; }
        public TypeNode Prototype { get; private set; }
        public TypeDefEntry Entry { get; private set; }
        public ITypeEntry? BaseType { get; private set; }
        public TypeDefinition TypeDef { get; private set; }
        public ImmutableArray<ITypeEntry> TypeArguments { get; private set; }
        public ImmutableArray<FieldDefEntry> Fields { get; private set; }
        public ImmutableArray<MethodDefEntry> Methods { get; private set; }
        public ImmutableArray<MethodDefEntry> VirtualMethods { get; private set; }
        //public ImmutableArray<PropertyDesc> Properties { get; }
        public HashSet<ITypeDeriveSource> DerivedEntry { get; private set; }
        public ImmutableArray<(MethodDefEntry ctor, CustomAttributeValue<ITypeEntry> arguments)> CustomAttributes { get; private set; }
        public bool IsValid => Entry is not null;
        public void CreateDetails(TypeDefEntry entry)
        {
            var metaReader = entry.InvariantPart.AsmInfo.MetaReader;
            var typeDef = entry.InvariantPart.TypeDef;
            var typeEnv = entry.TypeEnv;

            Entry = entry;
            TypeDef = metaReader.GetTypeDefinition(typeDef);

            Attributes = entry.Attributes;
            Name = metaReader.GetString(TypeDef.Name);

            Prototype = typeEnv.NamespaceTree[entry.InvariantPart.AsmInfo.Token].TypeNodes[typeDef];

            TypeArguments = TypeDef.GetGenericParameters().Select(e =>
            {
                return (ITypeEntry)GenericPlaceholderTypeEntry.Create(typeEnv.EntryManager, entry, e);
            }).ToImmutableArray();

            Fields = TypeDef.GetFields().Select(e =>
            {
                return FieldDefEntry.Create(typeEnv.EntryManager, entry.AsmInfo, e);
            }).ToImmutableArray();

            Methods = TypeDef.GetMethods().Select(e =>
            {
                return MethodDefEntry.Create(typeEnv.EntryManager, entry.AsmInfo, e);
            }).ToImmutableArray();


            DerivedEntry = [.. TypeArguments, .. Fields.Select(e => e.GetDetails().FieldType)];



            if (!TypeDef.BaseType.IsNil)
            {
                BaseType = typeEnv.TokenResolver.ResolveTypeToken(entry.AsmInfo, TypeDef.BaseType, new GenericTypeContext(TypeArguments, []));
                DerivedEntry.Add(BaseType);
            }


            if(BaseType is TypeDefEntry baseEntry) {
                var baseFullName = baseEntry.GetDetails().Prototype.FullName;
                IsValueType = baseFullName switch {
                    ".System.Enum" => true,
                    ".System.ValueType" => true,
                    _ => BaseType.IsValueType
                };
                IsEnum = baseFullName == ".System.Enum";
            } else {
                if(Prototype.FullName == ".System.Object" || Prototype.FullName == ".<Module>") {
                    IsValueType = false;
                } else {
                    throw new NotSupportedException();
                }
            }

            VirtualMethods = Methods.Where(e => e.GetDetails().MethodDef.Attributes.HasFlag(MethodAttributes.Virtual)).ToImmutableArray();

            CustomAttributes = TypeDef.GetCustomAttributes().Select(e => {
                var customAttribute = entry.AsmInfo.MetaReader.GetCustomAttribute(e);
                var attribValue = customAttribute.DecodeValue(typeEnv.SignDecoder);
                var ctor = (MethodDefEntry)typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, customAttribute.Constructor, GenericTypeContext.EmptyContext, out _);
                return (ctor, arguments: attribValue);
            }).ToImmutableArray();

        }
    }
}
