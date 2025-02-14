using Microsoft.VisualBasic;
using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Linq;

namespace Liella.TypeAnalysis {
    public class AssemblyReaderTuple: IDisposable {
        private bool m_HasDisposed;
        public PEReader BinaryReader { get; }
        public MetadataReader MetaReader { get; }
        public bool IsPruneEnabled { get; }
        public AssemblyToken Token { get; }
        public AssemblyReaderTuple(Stream assemblyStream, bool isPruneEnabled) {
            BinaryReader = new(assemblyStream);
            MetaReader = BinaryReader.GetMetadataReader();
            IsPruneEnabled = isPruneEnabled;

            var asmDef = MetaReader.GetAssemblyDefinition();
            Token = new(MetaReader, asmDef);

        }

        protected virtual void Dispose(bool disposing) {
            if(!m_HasDisposed) {
                if(disposing) {
                    BinaryReader.Dispose();
                }
                m_HasDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
    public enum EntityEntryCode:int {
        NonGenericEntry = 0x0,
        GenericEntry = 0x1,
        NonGenericMethodEntry = 0x2,
        GenericMethodEntry = 0x3,
        PointerEntry = 0x4,
        PrimitiveEntry = 0x5,
        FieldEntry = 0x6,
        GenericPlaceholder = 0x7
    }
    public class NamespaceQueryTree {
        public NamespaceNode RootNamespace { get; }
        public List<TypeNode> AllTypes { get; } = new();
        public AssemblyReaderTuple AssemblyInfo { get; }
        public NamespaceQueryTree(AssemblyReaderTuple asmInfo) {
            AssemblyInfo = asmInfo;
            RootNamespace = new("", asmInfo, null);

            var rootNamespace = asmInfo.MetaReader.GetNamespaceDefinitionRoot();
            ImportNamespace(rootNamespace, RootNamespace);
        }
        public NamespaceNodeBase this[string name, bool isNamespace = true] {
            get => RootNamespace[name, isNamespace];
        }
        public TypeNode FindTypeNode(string nsName, string typeName) {
            var nsSections = nsName.Split('.');
            var currentNode = (NamespaceNodeBase)RootNamespace;
            foreach(var i in nsSections) {
                currentNode = currentNode[i];
            }
            return (TypeNode)currentNode[typeName, false];
        }


        protected void ImportNamespace(NamespaceDefinition nsDef, NamespaceNodeBase currentNode) {
            var metaReader = AssemblyInfo.MetaReader;

            foreach(var i in nsDef.NamespaceDefinitions) {
                var subNs = metaReader.GetNamespaceDefinition(i);
                var subNsName = metaReader.GetString(subNs.Name);

                if(!currentNode.TryGetNamespace(subNsName, out var subNsNode)) {
                    currentNode[subNsName, true] = subNsNode = new NamespaceNode(subNsName, AssemblyInfo, currentNode);
                }

                ImportNamespace(subNs, subNsNode!);
            }

            foreach(var i in nsDef.TypeDefinitions) {
                var subType = metaReader.GetTypeDefinition(i);
                var subTypeName = metaReader.GetString(subType.Name);

                if(!currentNode.TryGetType(subTypeName, out var subTypeNode)) {
                    currentNode[subTypeName, false] = subTypeNode = new TypeNode(subTypeName, currentNode, AssemblyInfo, subType);
                }

                AllTypes.Add((TypeNode)subTypeNode!);
                ImportType(subType, subTypeNode!);
            }
        }

        protected void ImportType(TypeDefinition nsDef, NamespaceNodeBase currentNode) {
            var metaReader = AssemblyInfo.MetaReader;

            foreach(var i in nsDef.GetNestedTypes()) {
                var subType = metaReader.GetTypeDefinition(i);
                var subTypeName = metaReader.GetString(subType.Name);

                if(!currentNode.TryGetType(subTypeName, out var subTypeNode)) {
                    currentNode[subTypeName, false] = subTypeNode = new TypeNode(subTypeName, currentNode, AssemblyInfo, subType);
                }

                AllTypes.Add((TypeNode)subTypeNode!);
                ImportType(subType, subTypeNode!);
            }
        }
    }
    public interface IEntityEntry:IEquatable<IEntityEntry> {
        string Name { get; }
        string FullName { get; }
        bool IsComplete { get; }
        bool IsGenericInstantiation { get; }
        AssemblyReaderTuple AsmInfo { get; }
        ImmutableArray<TypeEntry?> TypeGenericParams { get; }
        ImmutableArray<TypeEntry?> MethodGenericParams { get; }
        IEnumerable<IEntityEntry> DerivedEntities { get; }
        void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector);
        IEntityEntry Clone();

        public SignatureGenericContext GetGenericContext()
            => new(TypeGenericParams, MethodGenericParams);
    }
    public class GenericPlaceholderTypeEntry : TypeEntry {
        [ThreadStatic]
        private static GenericPlaceholderTypeEntry? m_HashKey;
        public GenericPlaceholderTypeEntry(string name, IEntityEntry parent) : base(name) {
            Parent = parent;
        }
        public IEntityEntry Parent { get; protected set; }
        public override string FullName => Name;
        public override AssemblyReaderTuple AsmInfo => Parent.AsmInfo;
        public override bool IsComplete => false;
        public override bool IsGenericInstantiation => false;
        public override ImmutableArray<TypeEntry?> TypeGenericParams { 
            get =>  ImmutableArray<TypeEntry?>.Empty; 
            protected set => throw new NotImplementedException(); 
        }
        public override ImmutableArray<TypeEntry?> MethodGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotImplementedException(); 
        }

        public override void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
        }

        public override IEntityEntry Clone() {
            return new GenericPlaceholderTypeEntry(Name, Parent);
        }

        public override bool Equals(IEntityEntry? other) {
            if(other is GenericPlaceholderTypeEntry placeholder) {
                return placeholder.Parent.Equals(Parent) && placeholder.Name.Equals(Name);
            }
            return false;
        }

        public override FieldEntry GetField(string name, TypeEnvironment typeEnv) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(Parent);
            hashCode.Add(Name);
            return (hashCode.ToHashCode() << 3) | (int)EntityEntryCode.GenericPlaceholder;
        }
        public static GenericPlaceholderTypeEntry Create(EntityEntryManager manager, string name, IEntityEntry parent) {
            if(m_HashKey is null) m_HashKey = new(name, parent);
            m_HashKey.Name = name;
            m_HashKey.Parent = parent;
            return manager.GetEntryOrAdd(m_HashKey);
        }
        public override MethodEntry GetMethod(MethodDefinitionHandle methodDef, TypeEnvironment typeEnv) {
            throw new NotImplementedException();
        }

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv, MethodSignature<TypeEntry> signature, ImmutableArray<TypeEntry> methodGenerics) {
            throw new NotImplementedException();
        }

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv) {
            throw new NotImplementedException();
        }
    }
    public class MethodDetails {
        public MethodEntry Entry { get; }
        public MethodDefinition MethodDef => Entry.MethodDef;
        public ILDecoder? ILCode { get; }
        public MethodSignature<TypeEntry> Signature { get; }
        public ImmutableArray<TypeEntry> GenericPlaceholders { get; }
        public ImmutableArray<TypeEntry> ParamTypes => Signature.ParameterTypes;
        public TypeEntry ReturnType => Signature.ReturnType;
        public MethodDetails(MethodEntry entry, TypeEnvironment typeEnv) {
            Entry = entry;

            var asmInfo = entry.AsmInfo;
            var binReader = asmInfo.BinaryReader;
            var metaReader = asmInfo.MetaReader;
            var methodDef = entry.MethodDef;

            if(methodDef.RelativeVirtualAddress != 0) {
                var methodBody = binReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                var ilCode = methodBody.GetILContent();
                ILCode = new(ilCode);
            }

            GenericPlaceholders = methodDef.GetGenericParameters().Select(e => {
                var genericParamPlaceholder = metaReader.GetGenericParameter(e);
                var name = metaReader.GetString(genericParamPlaceholder.Name);

                return (TypeEntry)GenericPlaceholderTypeEntry.Create(typeEnv.EntryManager, name, entry);
            }).ToImmutableArray();


            Signature = methodDef.DecodeSignature(typeEnv.SignDecoder, new(entry.TypeGenericParams, GenericPlaceholders!));
        }

    }
    public class ILDecoder:IEnumerable<(ILOpCode opcode, ulong operand)> {
        private static Dictionary<ILOpCode, OpCode> m_OpCodeMap = new();
        private static Dictionary<ILOpCode, int> m_OpCodeSizeMap = new();
        public static Dictionary<ILOpCode, OpCode> OpCodeMap => m_OpCodeMap;
        static ILDecoder() {
            foreach(var i in typeof(OpCodes).GetFields()) {
                var value = (OpCode)(i.GetValue(null) ?? default(OpCode));
                m_OpCodeMap.Add((ILOpCode)value.Value, value);

                switch(value.OperandType) {
                    case OperandType.InlineNone: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 0);
                        break;
                    }
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 1);
                        break;
                    }
                    case OperandType.InlineSwitch: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 4);
                        break;
                    }

                    case OperandType.InlineVar: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 2);
                        break;
                    }
                    case OperandType.ShortInlineR:
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineI:
                    case OperandType.InlineMethod:
                    case OperandType.InlineSig:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 4);
                        break;
                    }
                    case OperandTypfe.InlineI8:
                    case OperandType.InlineR: {
                        m_OpCodeSizeMap.Add((ILOpCode)value.Value, 8);
                        break;
                    }
                    default: throw new NotImplementedException();
                }
            }
        }

        protected ImmutableArray<byte> m_ILCodes = ImmutableArray<byte>.Empty;
        protected ImmutableArray<(ILOpCode opcode, ulong operand)> m_Insts;
        public ILDecoder(ImmutableArray<byte> ilCode) {
            m_ILCodes = ilCode;

            var codeSpan = ilCode.AsSpan();
            var instBuilder = ImmutableArray.CreateBuilder<(ILOpCode opcode, ulong operand)>();

            for(var i = 0; i < ilCode.Length;) {
                var (opcode, operand, size) = DecodeSingleOpCode(codeSpan.Slice(i));
                instBuilder.Add((opcode, operand));

                i += size;
            }

            m_Insts = instBuilder.ToImmutable();
        }
        protected static (ILOpCode code, ulong operand, int length) DecodeSingleOpCode(ReadOnlySpan<byte> code) {
            var i = 0;
            var ilOpcode = (ILOpCode)code[i++];
            if(((uint)ilOpcode) >= 249) {
                ilOpcode = (ILOpCode)((((uint)ilOpcode) << 8) + code[i++]);
            }
            var opcode = m_OpCodeMap[ilOpcode];


            var operandSize = opcode.OperandType switch {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or
                OperandType.ShortInlineVar => 1,

                OperandType.InlineVar => 2,

                OperandType.InlineField or OperandType.ShortInlineR or
                OperandType.InlineI or OperandType.InlineMethod or
                OperandType.InlineSig or OperandType.InlineString or
                OperandType.InlineTok or OperandType.InlineType or
                OperandType.InlineBrTarget => 4,

                OperandType.InlineI8 or
                OperandType.InlineR => 8,

                _ => throw new NotImplementedException()

            };

            var operand = operandSize switch {
                0 => 0ul,
                1 => (ulong)(sbyte)code[i],
                2 => (ulong)MemoryMarshal.AsRef<short>(code.Slice(i, 2)),
                4 => (ulong)MemoryMarshal.AsRef<int>(code.Slice(i, 4)),
                8 => (ulong)MemoryMarshal.AsRef<long>(code.Slice(i, 8)),
                _ => throw new NotSupportedException()
            };

            return (ilOpcode, operand, i + operandSize);

        }

        public IEnumerator<(ILOpCode opcode, ulong operand)> GetEnumerator()
            => ((IEnumerable<(ILOpCode opcode, ulong operand)>)m_Insts).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class GenericMethodEntry : MethodEntry {
        [ThreadStatic]
        private static GenericMethodEntry? m_HashKey;
        public override bool IsGenericInstantiation => true;
        public override ImmutableArray<TypeEntry?> MethodGenericParams {
            get; protected set;
        }
        public override bool IsComplete
            => MethodGenericParams.All(e=>e?.IsComplete ?? false);
        public override string FullName 
            => $"{base.FullName}[{MethodGenericParams.Select(e=>e?.FullName ?? "<None>").Aggregate((u,v)=>$"{u}, {v}")}]";
        protected GenericMethodEntry(TypeEntry entry, MethodDefinitionHandle methodDef, ImmutableArray<TypeEntry?> typeParams) : base(entry, methodDef) {
            MethodGenericParams = typeParams;
            if(Name == "F2") Debugger.Break();
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(DeclType);
            hash.Add(MethodDefHandle);
            foreach(var i in MethodGenericParams) hash.Add(i);
            return (hash.ToHashCode() << 3) | (int)EntityEntryCode.GenericMethodEntry;
        }

        public override bool Equals(IEntityEntry? other) {
            if(other is GenericMethodEntry genericEntry) {
                return base.Equals(other) && 
                    MethodGenericParams.SequenceEqual(genericEntry.MethodGenericParams);
            }
            return false;
           
        }

        private static GenericMethodEntry GetHashKeyObject(TypeEntry declType, MethodDefinitionHandle methodDef, ImmutableArray<TypeEntry?> typeParams) {
            if(m_HashKey is null) m_HashKey = new(declType, methodDef, typeParams);
            m_HashKey.DeclType = declType;
            m_HashKey.MethodDefHandle = methodDef;
            m_HashKey.MethodGenericParams = typeParams;
            return m_HashKey;
        }
        public static GenericMethodEntry CreateEntry(EntityEntryManager manager, TypeEntry declType, MethodDefinitionHandle methodDef, ImmutableArray<TypeEntry?> typeParams) {
            var keyEntry = GetHashKeyObject(declType, methodDef, typeParams);
            return (GenericMethodEntry)manager.GetEntryOrAdd(keyEntry);
        }
        public override IEntityEntry Clone() {
            return new GenericMethodEntry(DeclType, MethodDefHandle, MethodGenericParams);
        }
        public override void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
            base.ActivateEntityImpl(typeEnv, collector);
        }
    }
    public class MethodEntry: IEntityEntry {
        [ThreadStatic]
        private static MethodEntry? m_HashKey;
        public TypeEntry DeclType { get; protected set; }
        public MethodDefinitionHandle MethodDefHandle { get; protected set; }
        public MethodDetails? Details { get; protected set; }
        protected HashSet<IEntityEntry> m_DerivedEntities = new();
        public IEnumerable<IEntityEntry> DerivedEntities => m_DerivedEntities;
        public virtual bool IsGenericInstantiation => false;
        public virtual ImmutableArray<TypeEntry?> MethodGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotImplementedException();
        }
        public ImmutableArray<TypeEntry?> TypeGenericParams
            => DeclType.TypeGenericParams;
        public SignatureGenericContext GenericContext
            => new(TypeGenericParams, MethodGenericParams);
        public virtual bool IsComplete
            => MethodDef.GetGenericParameters().Count == 0;
        public virtual string FullName {
            get => $"{DeclType.FullName}::{Name}";
        }
        public MethodDefinition MethodDef
            => DeclType.AsmInfo.MetaReader.GetMethodDefinition(MethodDefHandle);
        public AssemblyReaderTuple AsmInfo => DeclType.AsmInfo;
        public string Name => DeclType.AsmInfo.MetaReader.GetString(MethodDef.Name);
        protected MethodEntry(TypeEntry entry, MethodDefinitionHandle methodDef) {
            DeclType = entry;
            MethodDefHandle = methodDef;
        }

        public override string ToString() => FullName;
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(DeclType);
            hash.Add(MethodDefHandle);
            return (hash.ToHashCode() << 3) | ((int)EntityEntryCode.NonGenericMethodEntry);
        }

        public virtual bool Equals(IEntityEntry? other) {
            if(other is MethodEntry methodEntry)
                return (methodEntry?.DeclType.Equals(DeclType) ?? false) &&
                    methodEntry?.MethodDefHandle == MethodDefHandle;
            return false;
        }

        private static MethodEntry GetHashKeyObject(TypeEntry declType, MethodDefinitionHandle methodDef) {
            if(m_HashKey is null) m_HashKey = new(declType, methodDef);
            m_HashKey.DeclType = declType;
            m_HashKey.MethodDefHandle = methodDef;
            return m_HashKey;
        }
        public static MethodEntry CreateEntry(EntityEntryManager manager, TypeEntry declType, MethodDefinitionHandle methodDef) {
            var keyEntry = GetHashKeyObject(declType, methodDef);
            return manager.GetEntryOrAdd(keyEntry);
        }

        public bool IsMatchSignature(MethodSignature<TypeEntry> signature, TypeEnvironment typeEnv, SignatureGenericContext genericContext) {
            if(Details is null) Details = new(this, typeEnv);
            var sign = Details.MethodDef.DecodeSignature(typeEnv.SignDecoder, genericContext);

            return sign.RequiredParameterCount == signature.RequiredParameterCount &&
                sign.ParameterTypes.SequenceEqual(signature.ParameterTypes) &&
                sign.GenericParameterCount == signature.GenericParameterCount &&
                sign.ReturnType.Equals(signature.ReturnType);

        }
        protected void ActivateEntityImpl(TypeEnvironment typeEnv, TypeCollector collector) {
            if(Details is null) Details = new(this, typeEnv);

            foreach(var i in Details.ParamTypes) {
                collector.NotifyNewEntity(i);
            }
            collector.NotifyNewEntity(Details.ReturnType);

            if(Details.ILCode is not null) {
                foreach(var (opcode, operand) in Details.ILCode) {
                    var opcodeInfo = ILDecoder.OpCodeMap[opcode];
                    switch(opcodeInfo.OperandType) {
                        case OperandType.InlineField: {
                            var fieldToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                            var fieldEntry = typeEnv.ResolveFieldToken(AsmInfo, fieldToken, GenericContext);
                            break;
                        }
                        case OperandType.InlineType: {
                            var typeToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                            var refType = typeEnv.ResolveTypeToken(AsmInfo, typeToken, GenericContext);

                            collector.NotifyNewEntity(refType);

                            break;
                        }
                        case OperandType.InlineMethod: {
                            var methodToken = MetadataTokenHelpers.MakeEntityHandle((int)operand);

                            var refMethod = typeEnv.ResolveMethodToken(AsmInfo, methodToken, GenericContext);

                            collector.NotifyNewEntity(refMethod);
                            collector.NotifyNewEntity(refMethod.DeclType);

                            break;
                        }
                    }
                }
            }

        }
        public virtual void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
            if(MethodDef.GetGenericParameters().Count != 0) return; // Skip incomplete method actiavtion

            ActivateEntityImpl(typeEnv, collector);
        }

        public virtual IEntityEntry Clone() {
            return new MethodEntry(DeclType, MethodDefHandle);
        }
    }
    public class TypeCollector {
        public TypeEnvironment TypeEnv { get; }
        public HashSet<IEntityEntry> ActiveEntities { get; } = new();
        public IEnumerable<MethodEntry> ActiveMethods
            => ActiveEntities.OfType<MethodEntry>();
        public IEnumerable<TypeEntry> ActiveTypes
            => ActiveEntities.OfType<TypeEntry>();

        protected Queue<IEntityEntry> m_CollectPendingQueue = new();
        protected List<IEntityEntry> m_InstantiationPending = new();
        public TypeCollector(TypeEnvironment typeEnv) {
            TypeEnv = typeEnv;
        }
        public void CollectEntities(IEnumerable<IEntityEntry> collectRoot) {
            foreach(var i in collectRoot)
                m_CollectPendingQueue.Enqueue(i);

            while(m_CollectPendingQueue.Count != 0) {
                var currentEntity = m_CollectPendingQueue.Dequeue();

                if(ActiveEntities.Contains(currentEntity)) continue;

                currentEntity.ActivateEntity(TypeEnv, this);

                ActiveEntities.Add(currentEntity);
            }

            var declTypes = ActiveMethods.Select(e => e.DeclType).ToArray();

            foreach(var i in declTypes) {
                ActiveEntities.Add(i);
            }
        }
        public void NotifyNewEntity(IEntityEntry entry) {
            if(ActiveEntities.Contains(entry)) return;
            if(!entry.IsComplete) return;

            if(entry.IsGenericInstantiation) {
                m_InstantiationPending.Add(entry);
            } else {
                m_CollectPendingQueue.Enqueue(entry);
            }
        }
    }
    public record AssemblyToken(
        string Name, Version Version,
        string Culture, byte[] PublicKeyToken) {
        public AssemblyToken(MetadataReader metaReader,AssemblyDefinition asmDef) 
            : this(metaReader.GetString(asmDef.Name),
                  asmDef.Version, 
                  metaReader.GetString(asmDef.Culture),
                  metaReader.GetBlobBytes(asmDef.PublicKey)) { }
        public AssemblyToken(MetadataReader metaReader, AssemblyReference asmRef)
            : this(metaReader.GetString(asmRef.Name),
                  asmRef.Version,
                  metaReader.GetString(asmRef.Culture),
                  metaReader.GetBlobBytes(asmRef.PublicKeyOrToken)) { }
    }
    public class TypeEnvironment {
        protected Dictionary<AssemblyToken, AssemblyReaderTuple> m_Assemblies = new();
        public Dictionary<AssemblyToken, NamespaceQueryTree> NamespaceTree { get; } = new();
        public NamespaceQueryTree? SystemLibraryTree { get; protected set; } 
        public EntityEntryManager EntryManager { get; } 
        public TypeCollector TypeCollector { get; }
        public SignatureDecoder SignDecoder { get; }
        public TypeEnvironment() {
            EntryManager = new(this);
            TypeCollector = new(this);
            SignDecoder = new(this);
        }
        public AssemblyReaderTuple GetAsmInfo(MetadataReader metaReader) {
            foreach(var i in m_Assemblies.Values)
                if(i.MetaReader == metaReader)
                    return i;
            throw new NotImplementedException();
        }
        public AssemblyReaderTuple AddMainAssembly(Stream stream) {
            var readerTuple = new AssemblyReaderTuple(stream, false);
            m_Assemblies.Add(readerTuple.Token, readerTuple);
            return readerTuple;
        }
        public AssemblyReaderTuple AddDependencyLibrary(Stream stream) {
            var readerTuple = new AssemblyReaderTuple(stream, true);
            m_Assemblies.Add(readerTuple.Token, readerTuple);
            return readerTuple;
        }

        public void ImportTypes() {
            foreach(var i in m_Assemblies) {
                var namespaceTree = new NamespaceQueryTree(i.Value);
                NamespaceTree.Add(i.Key, namespaceTree);
            }
            foreach(var i in m_Assemblies) {
                var metaReader = i.Value.MetaReader;
                foreach(var j in metaReader.CustomAttributes) {
                    var asmAttribute = metaReader.GetCustomAttribute(j);
                    if(asmAttribute.Parent.Kind != HandleKind.AssemblyDefinition) continue;
                    var attributeConstructor = ResolveMethodToken(i.Value, asmAttribute.Constructor, SignatureGenericContext.EmptyContext);
                    
                    if(attributeConstructor.DeclType.FullName == ".System.Runtime.CompilerServices.SystemLibraryAttribute") {
                        if(SystemLibraryTree is not null)
                            throw new InvalidOperationException("Conflict system library");
                        SystemLibraryTree = NamespaceTree[i.Key];
                    }
                }
            }
        }

        public void CollectEntities() {
            var initialEntities = NamespaceTree.Where(e => {
                return !e.Value.AssemblyInfo.IsPruneEnabled;
            })
            .SelectMany(e=>e.Value.AllTypes)
            .Where(e=> {
                return e.ImageTypeDef.GetGenericParameters().Count == 0;
            }).Select(e=> NonGenericTypeEntry.CreateEntry(EntryManager, e));

            TypeCollector.CollectEntities(initialEntities);
        }
        public TypeNode ResolveTypeDefinition(AssemblyReaderTuple asmInfo, TypeDefinitionHandle typeDefHandle) {
            var metaReader = asmInfo.MetaReader;
            var typeDef = metaReader.GetTypeDefinition(typeDefHandle);
            var typeName = metaReader.GetString(typeDef.Name);

            if(!typeDef.IsNested) {
                var nsName = metaReader.GetString(typeDef.Namespace);
                return NamespaceTree[asmInfo.Token].FindTypeNode(nsName, typeName);
            } else {
                var parentNode = ResolveTypeDefinition(asmInfo, typeDef.GetDeclaringType());
                return (TypeNode)parentNode[typeName, false];
            }
        }
        public TypeNode ResolveTypeReference(AssemblyReaderTuple asmInfo, TypeReferenceHandle typeRefHandle) {
            var metaReader = asmInfo.MetaReader;
            var typeRef = metaReader.GetTypeReference(typeRefHandle);
            var resolveScope = typeRef.ResolutionScope;

            var nsName = metaReader.GetString(typeRef.Namespace);
            var typeName = metaReader.GetString(typeRef.Name);

            switch(resolveScope.Kind) {
                case HandleKind.AssemblyReference: {
                    var asmRef = metaReader.GetAssemblyReference((AssemblyReferenceHandle)resolveScope);
                    var asmToken = new AssemblyToken(metaReader, asmRef);
                    var namespaceTree = NamespaceTree[asmToken];
                    return namespaceTree.FindTypeNode(nsName, typeName);
                }
                case HandleKind.TypeReference: {
                    var parentType = ResolveTypeReference(asmInfo, (TypeReferenceHandle)resolveScope);
                    return (TypeNode)parentType[typeName, false];
                }
            }
            throw new NotImplementedException();
        }
        public FieldEntry ResolveFieldToken(AssemblyReaderTuple asmInfo, EntityHandle handle, SignatureGenericContext genericContext) {
            var metaReader = asmInfo.MetaReader;
            switch(handle.Kind) {
                case HandleKind.FieldDefinition: {
                    var fieldDef = metaReader.GetFieldDefinition((FieldDefinitionHandle)handle);
                    var fieldName = metaReader.GetString(fieldDef.Name);

                    var typeEntry = ResolveTypeToken(asmInfo, fieldDef.GetDeclaringType(), genericContext);
                    return typeEntry.GetField(fieldName, this);
                }
                case HandleKind.MemberReference: {
                    return (FieldEntry)ResolveMemberReference(asmInfo, (MemberReferenceHandle)handle, genericContext);
                }
            }
            throw new NotImplementedException();
        }
        public TypeEntry ResolveTypeToken(AssemblyReaderTuple asmInfo ,EntityHandle handle, SignatureGenericContext genericContext) {
            var metaReader = asmInfo.MetaReader;
            switch(handle.Kind) {
                case HandleKind.TypeDefinition: {
                    var typeNode = ResolveTypeDefinition(asmInfo, (TypeDefinitionHandle)handle);
                    return NonGenericTypeEntry.CreateEntry(EntryManager, typeNode);
                }
                case HandleKind.TypeReference: {
                    var typeNode = ResolveTypeReference(asmInfo, (TypeReferenceHandle)handle);
                    return NonGenericTypeEntry.CreateEntry(EntryManager, typeNode);
                }
                case HandleKind.TypeSpecification: {
                    var typeSpec = metaReader.GetTypeSpecification((TypeSpecificationHandle)handle);
                    var typeSign = typeSpec.DecodeSignature(SignDecoder, genericContext);

                    return typeSign;
                }
                default:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
        public MethodEntry ResolveMethodToken(AssemblyReaderTuple asmInfo, EntityHandle handle, SignatureGenericContext genericContext) {
            var metaReader = asmInfo.MetaReader;
            switch(handle.Kind) {
                case HandleKind.MemberReference:
                    return (MethodEntry)ResolveMemberReference(asmInfo, (MemberReferenceHandle)handle, genericContext);
                case HandleKind.MethodDefinition: {
                    var methodDef = metaReader.GetMethodDefinition((MethodDefinitionHandle)handle);
                    var typeDef = metaReader.GetTypeDefinition(methodDef.GetDeclaringType());

                    var nsName = metaReader.GetString(typeDef.Namespace);
                    var typeName = metaReader.GetString(typeDef.Name);

                    var typeNode = NamespaceTree[asmInfo.Token].FindTypeNode(nsName, typeName);
                    var typeEntry = NonGenericTypeEntry.CreateEntry(EntryManager, typeNode);

                    return typeEntry.GetMethod((MethodDefinitionHandle)handle, this);
                }

                case HandleKind.MethodSpecification: {
                    var methodSpec = metaReader.GetMethodSpecification((MethodSpecificationHandle)handle);

                    var methodGenerics = methodSpec.DecodeSignature(SignDecoder, genericContext);
                    var baseMethod = ResolveMethodToken(asmInfo, methodSpec.Method, new(genericContext.TypeGenericParams, methodGenerics!));
                    

                    return GenericMethodEntry.CreateEntry(EntryManager, baseMethod.DeclType, baseMethod.MethodDefHandle, methodGenerics!);
                }
                default:
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        public IEntityEntry ResolveMemberReference(AssemblyReaderTuple asmInfo, MemberReferenceHandle handle, SignatureGenericContext genericContext) {
            var metaReader = asmInfo.MetaReader;
            var memberRef = metaReader.GetMemberReference(handle);
            var memberRefName = metaReader.GetString(memberRef.Name);
            switch(memberRef.GetKind()) {
                case MemberReferenceKind.Field: {
                    var declType = ResolveTypeToken(asmInfo, memberRef.Parent, genericContext);
                    var name = metaReader.GetString(memberRef.Name);

                    return declType.GetField(name, this);
                }
                case MemberReferenceKind.Method: {
                    var declType = ResolveTypeToken(asmInfo, memberRef.Parent, genericContext);

                    var signature = memberRef.DecodeMethodSignature(SignDecoder, new(declType.TypeGenericParams, genericContext.MethodGenericParams));
                    var name = metaReader.GetString(memberRef.Name);
                    

                    return declType.GetMethod(name, this, signature, genericContext.MethodGenericParams!);
                }
            }

            throw new NotImplementedException();
        }
        public TypeEntry ResolvePrimitiveType(PrimitiveTypeCode code) {
            return PrimitiveTypeEntry.Create(EntryManager, code);
        }
        
    }
    
    public interface ITypeContainerBase {
        string Name { get; }
        ref TypeContainerState TypeContainerData { get; }
        public virtual ITypeContainerBase this[string name, bool isNamespace] {
            get => GetSubNode(name, isNamespace);
            set => SetSubNode(name, isNamespace, value);
        }
        protected ITypeContainerBase GetSubNode(string name, bool isNamespace)
            => TypeContainerData.SubTypes[(name, isNamespace)];
        protected void SetSubNode(string name, bool isNamespace, ITypeContainerBase value) {
            var subTypeDict = TypeContainerData.SubTypes;
            if(!subTypeDict.ContainsKey((name, isNamespace))) {
                subTypeDict.Add((name, isNamespace), value);
            } else {
                subTypeDict[(name, isNamespace)] = value;
            }
        }
        public struct TypeContainerState {
            public readonly Dictionary<(string name, bool isNamespace), ITypeContainerBase> SubTypes;
            public ITypeContainerBase? Parent { get; }
            public TypeContainerState(ITypeContainerBase? parent) {
                SubTypes = new();
                Parent = parent;
            }
        }
    }

    public abstract class NamespaceNodeBase: IEnumerable<NamespaceNodeBase> {
        protected Dictionary<(string name, bool isNamespace), NamespaceNodeBase> m_SubTypes = new();
        public string Name { get; }
        public NamespaceNodeBase? Parent { get; }
        public AssemblyReaderTuple AsmInfo { get; }
        public NamespaceNodeBase(string name, AssemblyReaderTuple asmInfo, NamespaceNodeBase? parent) {
            Name = name;
            AsmInfo = asmInfo;
            Parent = parent;
        }
        public virtual NamespaceNodeBase this[string name, bool isNamespace = true] {
            get => m_SubTypes[(name, isNamespace)];
            set {
                if(!m_SubTypes.ContainsKey((name, isNamespace))) {
                    m_SubTypes.Add((name, isNamespace), value);
                } else {
                    m_SubTypes[(name, isNamespace)] = value;
                }
            }
        }
        public virtual bool ContainsNamespace(string name) => m_SubTypes.ContainsKey((name, true));
        public virtual bool ContainsType(string name) => m_SubTypes.ContainsKey((name, false));
        public virtual bool TryGetNamespace(string name, out NamespaceNodeBase? value) => m_SubTypes.TryGetValue((name, true), out value);
        public virtual bool TryGetType(string name, out NamespaceNodeBase? value) => m_SubTypes.TryGetValue((name, false), out value);

        public IEnumerator<NamespaceNodeBase> GetEnumerator() => m_SubTypes.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class NamespaceNode: NamespaceNodeBase {
        public NamespaceNode(string name,AssemblyReaderTuple asmInfo, NamespaceNodeBase? parent) : base(name, asmInfo, parent) {
        }
    }
    public class TypeNode : NamespaceNodeBase {
        public TypeDefinition ImageTypeDef { get; }
        public override NamespaceNodeBase this[string name, bool isNamespace] {
            get => isNamespace ? 
                throw new ArgumentException("Namespace cannot be nested") : base[name, isNamespace];
            set => base[name, isNamespace] = isNamespace ? 
                throw new ArgumentException("Namespace cannot be nested") : value;
        }
        public string FullName {
            get {
                var result = new List<string>();
                var currentNode = (NamespaceNodeBase)this;
                while(currentNode is not null) {
                    result.Add(currentNode.Name);
                    currentNode = currentNode.Parent;
                }
                result.Reverse();
                return result.Aggregate((u, v) => $"{u}.{v}");
            }
        }
        public TypeNode(string name, NamespaceNodeBase? parent, AssemblyReaderTuple asmInfo, TypeDefinition typeDef) :base(name, asmInfo, parent) {
            ImageTypeDef = typeDef;
        }
    }
    public struct SignatureGenericContext {
        public ImmutableArray<TypeEntry?> TypeGenericParams { get; }
        public ImmutableArray<TypeEntry?> MethodGenericParams { get; }
        public static SignatureGenericContext EmptyContext { get; }
            = new(ImmutableArray<TypeEntry?>.Empty, ImmutableArray<TypeEntry?>.Empty);
        public SignatureGenericContext(ImmutableArray<TypeEntry?> typeGenerics, ImmutableArray<TypeEntry?> methodGenerics) {
            TypeGenericParams = typeGenerics;
            MethodGenericParams = methodGenerics;
        }
    }
    
    public class SignatureDecoder : ISignatureTypeProvider<TypeEntry, SignatureGenericContext> {
        public TypeEnvironment TypeEnv { get; }
        public SignatureDecoder(TypeEnvironment typeEnv) {
            TypeEnv = typeEnv;
        }

        public TypeEntry GetArrayType(TypeEntry elementType, ArrayShape shape) {
            throw new NotImplementedException();
        }

        public TypeEntry GetByReferenceType(TypeEntry elementType) {
            throw new NotImplementedException();
        }

        public TypeEntry GetFunctionPointerType(MethodSignature<TypeEntry> signature) {
            throw new NotImplementedException();
        }

        public TypeEntry GetGenericInstantiation(TypeEntry genericType, ImmutableArray<TypeEntry> typeArguments) {
            if(genericType is NonGenericTypeEntry nonGen) {
                return GenericTypeEntry.CreateEntry(TypeEnv.EntryManager, nonGen.Prototype, typeArguments!);
            } else {
                throw new NotImplementedException();
            }
        }

        public TypeEntry GetGenericMethodParameter(SignatureGenericContext genericContext, int index) {
            return genericContext.MethodGenericParams[index] ?? throw new NullReferenceException();
        }

        public TypeEntry GetGenericTypeParameter(SignatureGenericContext genericContext, int index) {
            return genericContext.TypeGenericParams[index] ?? throw new NullReferenceException();
        }

        public TypeEntry GetModifiedType(TypeEntry modifier, TypeEntry unmodifiedType, bool isRequired) {
            throw new NotImplementedException();
        }

        public TypeEntry GetPinnedType(TypeEntry elementType) {
            throw new NotImplementedException();
        }

        public TypeEntry GetPointerType(TypeEntry elementType) {
            return PointerTypeEntry.CreateEntry(TypeEnv.EntryManager, elementType);
        }

        public TypeEntry GetPrimitiveType(PrimitiveTypeCode typeCode) {
            return TypeEnv.ResolvePrimitiveType(typeCode);
        }

        public TypeEntry GetSZArrayType(TypeEntry elementType) {
            throw new NotImplementedException();
        }

        public TypeEntry GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) {
            return TypeEnv.ResolveTypeToken(TypeEnv.GetAsmInfo(reader), handle, SignatureGenericContext.EmptyContext);
        }

        public TypeEntry GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {
             return TypeEnv.ResolveTypeToken(TypeEnv.GetAsmInfo(reader), handle, SignatureGenericContext.EmptyContext);
        }

        public TypeEntry GetTypeFromSpecification(MetadataReader reader, SignatureGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) {
            throw new NotImplementedException();
        }
    }

    public class FieldEntry : IEntityEntry, IEquatable<IEntityEntry> {
        private static FieldEntry? m_HashKey;
        public TypeEntry DeclType { get; protected set; }
        public string Name { get; protected set; }
        public bool IsComplete => DeclType.IsComplete && FieldType.IsComplete;
        public TypeEntry FieldType { get; protected set; }
        public FieldAttributes Attribute { get; protected set; }
        public string FullName => $"{DeclType}::{Name}";
        public bool IsGenericInstantiation => false;
        public IEnumerable<IEntityEntry> DerivedEntities
            => [FieldType];
        public AssemblyReaderTuple AsmInfo => DeclType.AsmInfo;

        public ImmutableArray<TypeEntry?> TypeGenericParams => DeclType.TypeGenericParams;

        public ImmutableArray<TypeEntry?> MethodGenericParams => ImmutableArray<TypeEntry?>.Empty;

        protected FieldEntry(TypeEntry declType, string name, TypeEntry fieldType, FieldAttributes attributes) {
            DeclType = declType;
            Name = name;
            FieldType = fieldType;
            Attribute = attributes;
        }

        public void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
            collector.NotifyNewEntity(DeclType);
            collector.NotifyNewEntity(FieldType);
        }

        public IEntityEntry Clone() {
            return new FieldEntry(DeclType, Name, FieldType, Attribute);
        }

        public bool Equals(IEntityEntry? other) {
            if(other is FieldEntry fieldEntry)
                return fieldEntry.DeclType.Equals(DeclType) && 
                    fieldEntry.Name.Equals(Name);
            return false;
        }
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(DeclType);
            hash.Add(Name);
            return (hash.ToHashCode() << 3) | (int)EntityEntryCode.FieldEntry;
        }
        public static FieldEntry Create(EntityEntryManager manger, TypeEntry declType, string name, TypeEntry fieldType, FieldAttributes attributes) {
            if(m_HashKey is null) m_HashKey = new(declType, name, fieldType, attributes);
            m_HashKey.DeclType = declType;
            m_HashKey.FieldType = fieldType;
            m_HashKey.Name = name;
            m_HashKey.Attribute = attributes;
            return manger.GetEntryOrAdd(m_HashKey);
        }
    }

    public record PropertyDesc(
        string Name,
        TypeEntry FieldType,
        TypeEntry DeclType,
        PropertyAttributes Attributes,
        MethodEntry? Getter,
        MethodEntry? Setter);
    public class TypeDetails {
        public TypeEntry Entry { get; }
        public ImmutableArray<FieldEntry> Fields { get; }
        public ImmutableArray<MethodEntry> Methods { get; }
        public ImmutableArray<PropertyDesc> Properties { get; }
        public ImmutableArray<GenericPlaceholderTypeEntry> GenericPlaceholders { get; }
        public TypeDetails(NonGenericTypeEntry entry, TypeEnvironment typeEnv) {
            Entry = entry;

            var asmInfo = entry.AsmInfo;
            var metaReader = asmInfo.MetaReader;
            var typeDef = entry.Prototype.ImageTypeDef;

            Fields = typeDef.GetFields().Select(e => {
                var fieldDef = metaReader.GetFieldDefinition(e);
                var fieldName = metaReader.GetString(fieldDef.Name);
                var fieldType = fieldDef.DecodeSignature(typeEnv.SignDecoder, entry.GenericContext);
                return FieldEntry.Create(typeEnv.EntryManager, entry, fieldName, fieldType, fieldDef.Attributes);
            }).ToImmutableArray();

            Methods = typeDef.GetMethods().Select(e => {
                return MethodEntry.CreateEntry(typeEnv.EntryManager, entry, e);
            }).ToImmutableArray();

            Properties = typeDef.GetProperties().Select(e => {
                var propDef = metaReader.GetPropertyDefinition(e);
                var propName = metaReader.GetString(propDef.Name);
                var propType = propDef.DecodeSignature(typeEnv.SignDecoder, entry.GenericContext);
                var accessor = propDef.GetAccessors();
                var getter = accessor.Getter.IsNil ? null : MethodEntry.CreateEntry(typeEnv.EntryManager, entry, accessor.Getter);
                var setter = accessor.Setter.IsNil ? null : MethodEntry.CreateEntry(typeEnv.EntryManager, entry, accessor.Setter);
                return new PropertyDesc(propName, propType.ReturnType, entry, propDef.Attributes, getter, setter);
            }).ToImmutableArray();

            GenericPlaceholders = typeDef.GetGenericParameters().Select(e => {
                var paramDef = metaReader.GetGenericParameter(e);
                var paramName = metaReader.GetString(paramDef.Name);

                return GenericPlaceholderTypeEntry.Create(typeEnv.EntryManager, paramName, entry);
            }).ToImmutableArray();

        }
    }
    public class PrimitiveTypeEntry : TypeEntry {
        [ThreadStatic]
        private static PrimitiveTypeEntry? m_HashKey;
        public PrimitiveTypeCode PrimitiveCode { get; protected set; }
        public NonGenericTypeEntry? PrimitiveBody { get; protected set; }
        public override bool IsComplete => true;
        public override bool IsGenericInstantiation => false;
        protected PrimitiveTypeEntry(PrimitiveTypeCode primitive) : base(primitive.ToString()) {
            PrimitiveCode = primitive;
        }

        public override string FullName => $"Primitive{Name}";

        public override AssemblyReaderTuple AsmInfo => throw new NotImplementedException();
        public override ImmutableArray<TypeEntry?> TypeGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotImplementedException(); 
        }
        public override ImmutableArray<TypeEntry?> MethodGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotImplementedException(); 
        }
        protected NonGenericTypeEntry EnsurePrimitiveBodyExist(TypeEnvironment typeEnv) {
            if(PrimitiveBody is null) {
                if(typeEnv.SystemLibraryTree is null)
                    throw new Exception("System library not found");

                var typeName = PrimitiveCode.ToString();
                var prototype = (TypeNode)typeEnv.SystemLibraryTree["System"][typeName, false];
                PrimitiveBody = NonGenericTypeEntry.CreateEntry(typeEnv.EntryManager, prototype);
            }
            return PrimitiveBody;
        }
        public override void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
            var primitiveBody = EnsurePrimitiveBodyExist(typeEnv);
            collector.NotifyNewEntity(primitiveBody);
        }

        public override IEntityEntry Clone() {
            return new PrimitiveTypeEntry(PrimitiveCode);
        }

        public override bool Equals(IEntityEntry? other) {
            if(other is PrimitiveTypeEntry pmtEntry)
                return pmtEntry.PrimitiveCode == PrimitiveCode;
            return false;
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(PrimitiveCode);
            return (hash.ToHashCode() << 3) | (int)EntityEntryCode.PrimitiveEntry;
        }

        public override MethodEntry GetMethod(MethodDefinitionHandle methodDef, TypeEnvironment typeEnv)
            => EnsurePrimitiveBodyExist(typeEnv).GetMethod(methodDef, typeEnv);

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv, MethodSignature<TypeEntry> signature, ImmutableArray<TypeEntry> methodGenerics)
            => EnsurePrimitiveBodyExist(typeEnv).GetMethod(name, typeEnv, signature, methodGenerics);

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv)
            => EnsurePrimitiveBodyExist(typeEnv).GetMethod(name, typeEnv);

        public static PrimitiveTypeEntry Create(EntityEntryManager entryManager, PrimitiveTypeCode primitive) {
            if(m_HashKey is null) m_HashKey = new(primitive);
            m_HashKey.PrimitiveCode = primitive;
            return entryManager.GetEntryOrAdd(m_HashKey);
        }

        public override FieldEntry GetField(string name, TypeEnvironment typeEnv)
            => EnsurePrimitiveBodyExist(typeEnv).GetField(name, typeEnv);
    }
    public abstract class TypeEntry : IEntityEntry, IEquatable<IEntityEntry> {
        protected HashSet<IEntityEntry> m_DerivedEntities = new();
        public IEnumerable<IEntityEntry> DerivedEntities => m_DerivedEntities;
        public abstract bool IsGenericInstantiation { get; }
        public virtual string Name { get; protected set; }
        public abstract string FullName { get; }
        public abstract bool IsComplete { get; }
        public abstract AssemblyReaderTuple AsmInfo { get; }
        public abstract ImmutableArray<TypeEntry?> TypeGenericParams { get; protected set; }
        public abstract ImmutableArray<TypeEntry?> MethodGenericParams { get; protected set; }

        public abstract void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector);
        public abstract IEntityEntry Clone();
        public abstract override int GetHashCode();
        public abstract bool Equals(IEntityEntry? other);

        public abstract FieldEntry GetField(string name, TypeEnvironment typeEnv);
        public abstract MethodEntry GetMethod(MethodDefinitionHandle methodDef, TypeEnvironment typeEnv);
        public abstract MethodEntry GetMethod(string name, TypeEnvironment typeEnv, MethodSignature<TypeEntry> signature, ImmutableArray<TypeEntry> methodGenerics);
        public abstract MethodEntry GetMethod(string name, TypeEnvironment typeEnv);
        public TypeEntry(string name) {
            Name = name;
        }
        public override string ToString() => FullName;
        public SignatureGenericContext GenericContext
            => new(TypeGenericParams, MethodGenericParams);
    }
    /// <summary>
    /// Type entry without evaluated generic param. Including non-generic types and generic prototypes
    /// </summary>
    public class NonGenericTypeEntry: TypeEntry {
        [ThreadStatic]
        private static NonGenericTypeEntry? m_HashKey;
        protected TypeDetails? m_Details;
        public override AssemblyReaderTuple AsmInfo => Prototype.AsmInfo;
        public TypeNode Prototype { get; protected set; }
        public override bool IsGenericInstantiation => false;
        public override bool IsComplete
            => Prototype.ImageTypeDef.GetGenericParameters().Count == 0;
        public override ImmutableArray<TypeEntry?> TypeGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotSupportedException();
        }
        public override ImmutableArray<TypeEntry?> MethodGenericParams {
            get => ImmutableArray<TypeEntry?>.Empty;
            protected set => throw new NotSupportedException();
        }
        public override string FullName => Prototype.FullName;
        protected NonGenericTypeEntry(TypeNode prototype):base(prototype.Name) {
            Prototype = prototype;
        }
        public TypeDetails GetDetails(TypeEnvironment typeEnv) {
            if(m_Details is null) m_Details = new(this, typeEnv);
            return m_Details;
        }
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Prototype);
            return (hash.ToHashCode() << 3) | ((int)EntityEntryCode.NonGenericEntry);
        }
        public static NonGenericTypeEntry CreateEntry(EntityEntryManager manager, TypeNode prototype) {
            if(m_HashKey is null) m_HashKey = new(prototype);
            m_HashKey.Prototype = prototype;
            return (NonGenericTypeEntry)manager.GetEntryOrAdd(m_HashKey);
        }

        public override bool Equals(IEntityEntry? other) {
            if(other is NonGenericTypeEntry nonGen)
                return nonGen?.Prototype == Prototype;
            return false;
        }

        public override void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {

            var asmInfo = AsmInfo;
            var typeDef = Prototype.ImageTypeDef;

            if(Details is null) Details = new(this, typeEnv);

            foreach(var i in Details.Fields) 
                collector.NotifyNewEntity(i.FieldType);
            foreach(var i in Details.Properties) 
                collector.NotifyNewEntity(i.FieldType);

            if(!asmInfo.IsPruneEnabled) {
                foreach(var i in Details.Methods)
                    collector.NotifyNewEntity(i);
                foreach(var i in Details.Properties) {
                    if(i.Getter is not null) collector.NotifyNewEntity(i.Getter);
                    if(i.Setter is not null) collector.NotifyNewEntity(i.Setter);
                }
            }
                
        }

        public override IEntityEntry Clone() {
            return new NonGenericTypeEntry(Prototype);
        }
        public override MethodEntry GetMethod(MethodDefinitionHandle methodDef, TypeEnvironment typeEnv) {
            if(Details is null) Details = new(this, typeEnv);

            foreach(var i in Details.Methods) {
                if(i.MethodDefHandle == methodDef) return i;
            }

            throw new KeyNotFoundException($"Unable to find method {methodDef}");
        }
        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv) {
            if(Details is null) Details = new(this, typeEnv);

            foreach(var i in Details.Methods) {
                if(i.Name != name) continue;
                return i;
            }

            throw new KeyNotFoundException($"Unable to find method {name}");
        }
        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv, MethodSignature<TypeEntry> signature, ImmutableArray<TypeEntry> methodGenerics) {
            if(Details is null) Details = new(this, typeEnv);

            var genericContext = new SignatureGenericContext(TypeGenericParams, methodGenerics!);

            foreach(var i in Details.Methods) {
                if(i.Name != name) continue;
                if(i.IsMatchSignature(signature, typeEnv, genericContext)) return i;
            }

            throw new KeyNotFoundException($"Unable to find method {name}");
        }

        public override FieldEntry GetField(string name, TypeEnvironment typeEnv) {
            if(Details is null) Details = new(this, typeEnv);

            foreach(var i in Details.Fields) {
                if(i.Name == name) return i;
            }

            throw new KeyNotFoundException($"Unable to find field {name}");
        }
    }
    public class PointerTypeEntry : TypeEntry {
        [ThreadStatic]
        private static PointerTypeEntry? m_HashKey;
        public TypeEntry BaseEntry { get; protected set; }
        public override string FullName => $"{BaseEntry.FullName}*";
        public override bool IsComplete => BaseEntry.IsComplete;
        public override bool IsGenericInstantiation => false;
        public override AssemblyReaderTuple AsmInfo => BaseEntry.AsmInfo;

        public override ImmutableArray<TypeEntry?> TypeGenericParams { 
            get => BaseEntry.TypeGenericParams;
            protected set => throw new NotSupportedException(); 
        }
        public override ImmutableArray<TypeEntry?> MethodGenericParams {
            get => BaseEntry.MethodGenericParams;
            protected set => throw new NotSupportedException();
        }

        protected PointerTypeEntry(TypeEntry baseEntry) : base($"{baseEntry.Name}*") {
            BaseEntry = baseEntry;
        }
        public override IEntityEntry Clone() {
            return new PointerTypeEntry(BaseEntry);
        }
        private static PointerTypeEntry GetHashKeyObject(TypeEntry baseEntry) {
            if(m_HashKey is null) m_HashKey = new(baseEntry);
            m_HashKey.BaseEntry = baseEntry;
            return m_HashKey;
        }
        public static PointerTypeEntry CreateEntry(EntityEntryManager manager, TypeEntry baseEntry) {
            var keyEntry = GetHashKeyObject(baseEntry);
            return (PointerTypeEntry)manager.GetEntryOrAdd(keyEntry);
        }
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(BaseEntry);
            return (hash.ToHashCode() << 3) | ((int)EntityEntryCode.PointerEntry);
        }
        public override bool Equals(IEntityEntry? other) {
            if(other is PointerTypeEntry ptrEntry)
                return ptrEntry.BaseEntry.Equals(BaseEntry);
            return false;
        }

        public override void ActivateEntity(TypeEnvironment typeEnv, TypeCollector collector) {
            collector.NotifyNewEntity(BaseEntry);
        }

        public override MethodEntry GetMethod(MethodDefinitionHandle methodDef, TypeEnvironment typeEnv) {
            throw new NotImplementedException();
        }

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv, MethodSignature<TypeEntry> signature, ImmutableArray<TypeEntry> methodGenerics) {
            throw new NotImplementedException();
        }

        public override MethodEntry GetMethod(string name, TypeEnvironment typeEnv) {
            throw new NotImplementedException();
        }

        public override FieldEntry GetField(string name, TypeEnvironment typeEnv) {
            throw new InvalidOperationException("Locate field in pointer type");
        }
    }
    public class GenericTypeEntry : NonGenericTypeEntry {
        [ThreadStatic]
        private static GenericTypeEntry? m_HashKey;
        public override ImmutableArray<TypeEntry?> TypeGenericParams { get; protected set; }
        public override bool IsComplete => TypeGenericParams.All(e => e.IsComplete);
        public override bool IsGenericInstantiation => true;
        public override string FullName {
            get => $"{base.FullName}[{TypeGenericParams.Select(e => e?.FullName ?? "<None>").Aggregate((u, v) => $"{u}, {v}")}]";
        }
        protected GenericTypeEntry(TypeNode prototype, ImmutableArray<TypeEntry?> typeParams) : base(prototype) {
            TypeGenericParams = typeParams;
        }
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Prototype);
            foreach(var i in TypeGenericParams) hash.Add(i);
            return (hash.ToHashCode() << 3) | ((int)EntityEntryCode.GenericEntry);
        }
        private static GenericTypeEntry GetHashKeyObject(TypeNode prototype, ImmutableArray<TypeEntry?> typeParams) {
            if(m_HashKey is null) m_HashKey = new(prototype, typeParams);
            m_HashKey.Prototype = prototype;
            m_HashKey.TypeGenericParams = typeParams;
            return m_HashKey;
        }
        public static TypeEntry CreateEntry(EntityEntryManager manager, TypeNode prototype, ImmutableArray<TypeEntry?> typeParams) {
            var keyEntry = GetHashKeyObject(prototype, typeParams);
            return manager.GetEntryOrAdd(keyEntry);
        }
        public override bool Equals(IEntityEntry? other) {
            if(other is GenericTypeEntry genericEntry)
                return genericEntry?.Prototype == Prototype && genericEntry.TypeGenericParams.SequenceEqual(TypeGenericParams);
            return false;
        }
        public override IEntityEntry Clone() {
            return new GenericTypeEntry(Prototype, TypeGenericParams);
        }
    }

    public class EntityEntryManager {
        public TypeEnvironment TypeEnv { get; }
        protected HashSet<IEntityEntry> m_EntrySet = new();
        public EntityEntryManager(TypeEnvironment typeEnv) {
            TypeEnv = typeEnv;
        }
        public T GetEntryOrAdd<T>(T key) where T:IEntityEntry {
            if(!m_EntrySet.TryGetValue(key, out var actualValue)) {
                m_EntrySet.Add(actualValue = key.Clone());
            }
            return (T)actualValue;
        }
    }

    public class ImageCompilationUnit {
        public TypeEnvironment TypeEnv { get; } = new();
        protected Func<AssemblyToken, Stream> m_ResolveCallback;
        public ImageCompilationUnit(Stream mainAssembly, Func<AssemblyToken, Stream> resolveCallback) {
            m_ResolveCallback = resolveCallback;

            var readerTuple = TypeEnv.AddMainAssembly(mainAssembly);
            ResolveDependency(readerTuple);
        }

        protected void ResolveDependency(AssemblyReaderTuple asmInfo) {
            foreach(var i in asmInfo.MetaReader.AssemblyReferences) {
                var asmRef = asmInfo.MetaReader.GetAssemblyReference(i);
                
                var asmFullName = new AssemblyToken(asmInfo.MetaReader, asmRef);

                var dependencyStream = m_ResolveCallback(asmFullName);

                var dependencyTuple = TypeEnv.AddDependencyLibrary(dependencyStream);

                ResolveDependency(dependencyTuple);
            }
        }

        public void BuildTypeEnvironment() {
            TypeEnv.ImportTypes();
            TypeEnv.CollectEntities();
        }
    }
    internal class Program {
        unsafe static void Main(string[] args) {

            var stream = new FileStream("./Payload.dll", FileMode.Open);
            var icu = new ImageCompilationUnit(stream, (asmName) => {
                return new FileStream($"./{asmName.Name}.dll", FileMode.Open);
            });

            icu.BuildTypeEnvironment();
            
            foreach(var i in icu.TypeEnv.TypeCollector.ActiveMethods) {
                Console.WriteLine(i);
            }
            foreach(var i in icu.TypeEnv.TypeCollector.ActiveTypes) {
                Console.WriteLine(i);
            }
        }
    }

    public static class MetadataTokenHelpers { 
        public static EntityHandle MakeEntityHandle(int metadataToken) {
            return Unsafe.BitCast<int, EntityHandle>(metadataToken);
        }
    }

}
