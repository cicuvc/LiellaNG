using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Namespaces;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;


namespace Liella.TypeAnalysis.Metadata.Entry
{
    
    public struct TypeDefDetails : IDetails<TypeDefEntry>
    {
        public TypeAttributes Attributes { get; private set; }
        public bool IsValueType { get; private set; }
        public bool IsEnum { get; private set; }
        public bool IsInterface => Attributes.HasFlag(TypeAttributes.Interface);
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
        public ImmutableArray<ITypeEntry> ImplInterfaces { get; private set; }
        public FrozenDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>> InterfaceImpl { get; private set; }
        public void CreateDetails(TypeDefEntry entry)
        {
            var metaReader = entry.InvariantPart.AsmInfo.MetaReader;
            var typeDef = entry.InvariantPart.TypeDef;
            var typeEnv = entry.TypeEnv;
            
            Entry = entry;
            TypeDef = metaReader.GetTypeDefinition(typeDef);

            Attributes = TypeDef.Attributes;
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

            var genericContext = new GenericTypeContext(TypeArguments, ImmutableArray<ITypeEntry>.Empty);

            ImplInterfaces = TypeDef.GetInterfaceImplementations().Select(e => {
                var impl = entry.AsmInfo.MetaReader.GetInterfaceImplementation(e);
                return typeEnv.TokenResolver.ResolveTypeToken(entry.AsmInfo, impl.Interface, genericContext);
            }).Distinct().ToImmutableArray();


            
            DerivedEntry = [.. TypeArguments,.. ImplInterfaces,.. Fields.Select(e => e.GetDetails().FieldType)];

            if (!TypeDef.BaseType.IsNil)
            {
                BaseType = typeEnv.TokenResolver.ResolveTypeToken(entry.AsmInfo, TypeDef.BaseType, genericContext);
                DerivedEntry.Add(BaseType);
            }


            if(BaseType is ITypeEntry baseEntry) {
                var baseFullName = baseEntry.FullName;
                IsValueType = baseFullName switch {
                    ".System.Enum@0" => true,
                    ".System.ValueType@0" => true,
                    _ => BaseType.IsValueType
                };
                IsEnum = baseFullName == ".System.Enum@0";
            } else {
                if(Prototype.FullName == ".System.Object" || Prototype.FullName == ".<Module>") {
                    IsValueType = false;
                } else if (Attributes.HasFlag(TypeAttributes.Interface)){
                    IsValueType = false;
                } else {
                    throw new NotSupportedException();
                }
            }

            VirtualMethods = Methods.Where(e => e.GetDetails().MethodDef.Attributes.HasFlag(MethodAttributes.Virtual)).ToImmutableArray();

            foreach(var i in VirtualMethods) {
                Debug.Assert(i.VirtualMethodPrototype is not null);

                DerivedEntry.Add(i.VirtualMethodPrototype);
                DerivedEntry.Add(i);
            }

            CustomAttributes = TypeDef.GetCustomAttributes().Select(e => {
                var customAttribute = entry.AsmInfo.MetaReader.GetCustomAttribute(e);
                var attribValue = customAttribute.DecodeValue(typeEnv.SignDecoder);
                var ctor = (MethodDefEntry)typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, customAttribute.Constructor, GenericTypeContext.EmptyContext, out _);
                return (ctor, arguments: attribValue);
            }).ToImmutableArray();

            // Handle special method impl resolution

            if(!IsInterface) {
                var interfaceImpls = ImplInterfaces.ToDictionary(e => e, e => new List<(IMethodEntry methodDecl, IMethodEntry methodImpl)>());

                foreach(var i in TypeDef.GetMethodImplementations()) {
                    var impl = entry.AsmInfo.MetaReader.GetMethodImplementation(i);

                    var methodBody = typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, impl.MethodBody, new(entry.TypeArguments, entry.MethodArguments), out var exactDecl);
                    var methodDecl = typeEnv.TokenResolver.ResolveMethodToken(entry.AsmInfo, impl.MethodDeclaration, new(entry.TypeArguments, entry.MethodArguments), out var exactMethodDecl);

                    if(!interfaceImpls.TryGetValue(exactMethodDecl, out var implList)) {
                        interfaceImpls.Add(exactMethodDecl, implList = new());
                    }
                    implList.Add((methodDecl, methodImpl: methodBody));
                }

                var exactCurrentType = (ITypeEntry)Entry;
                if(TypeArguments.Length != 0) {
                    exactCurrentType = TypeInstantiationEntry.Create(typeEnv.EntryManager, Entry, TypeArguments, true);
                }


                foreach(var interfaceType in ImplInterfaces) {
                    var implList = interfaceImpls[interfaceType];
                    var genericLookupTable = interfaceType is TypeInstantiationEntry interfaceInst ? 
                        interfaceInst.FormalArguments.Zip(interfaceInst.ActualArguments)
                        .ToFrozenDictionary(e => e.First, e => e.Second) 
                        : FrozenDictionary<ITypeEntry, ITypeEntry>.Empty;


                    foreach(var j in interfaceType.TypeMethods) {
                        if(implList.Any(e => e.methodDecl == j)) continue;

                        // All generics used in interface declared methods are present in interface declared generics
                        var targetSignature = SubsituteSignauture(genericLookupTable, j.GetDetails().Signature);

                        var lookupContext = new GenericTypeContext(genericContext.TypeArguments, j.MethodArguments);
                        var localMethod = FindInterfaceImplMethodInList(Methods, j.Name, targetSignature, lookupContext);

                        if(localMethod is null) {
                            var baseType = BaseType;
                            while((baseType is not null) && (localMethod is null)) {
                                localMethod = FindInterfaceImplMethodInList(baseType.TypeMethods, j.Name, targetSignature, lookupContext);
                                baseType = baseType.BaseType;
                            }
                        }

                        var decoratedPrototype = (IMethodEntry)j;

                        if(interfaceType is TypeInstantiationEntry interfaceTypeInst) {
                            decoratedPrototype = MethodInstantiation.Create(typeEnv.EntryManager, interfaceTypeInst, j, j.GetDetails().MethodGenericParams);
                        }

                        if(localMethod is not null) {
                            typeEnv.Collector.RegisterVirtualChain((MethodDefEntry)localMethod, j);

                            implList.Add((decoratedPrototype, localMethod));

                            DerivedEntry.Add(j);
                        } else {
                            throw new NotSupportedException("Unknown impl");
                        }
                    }
                }

                InterfaceImpl = interfaceImpls.Select(e=>new KeyValuePair<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>(e.Key, e.Value.ToImmutableArray())).ToFrozenDictionary();
            } else {
                InterfaceImpl = FrozenDictionary<ITypeEntry, ImmutableArray<(IMethodEntry methodDecl, IMethodEntry methodImpl)>>.Empty;
            }

        }
        private static MethodSignature<ITypeEntry> SubsituteSignauture(IReadOnlyDictionary<ITypeEntry, ITypeEntry> lut, MethodSignature<ITypeEntry> signature) {
            return new MethodSignature<ITypeEntry>(
                signature.Header, 
                lut.GetValueOrDefault(signature.ReturnType, signature.ReturnType), 
                signature.RequiredParameterCount, 
                signature.GenericParameterCount, 
                signature.ParameterTypes.Select(e => lut.GetValueOrDefault(e, e)).ToImmutableArray());
        }

        private static IMethodEntry? FindInterfaceImplMethodInList(IEnumerable<MethodDefEntry> methods, string name, MethodSignature<ITypeEntry> signature, GenericTypeContext genericContext) {
            foreach(var i in methods) {
                if(i.Name != name) continue;
                if(i.IsMatchSignature(signature, genericContext)) return i;
            }
            return null;
        }
    }
}
