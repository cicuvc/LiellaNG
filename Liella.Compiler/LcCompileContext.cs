using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.Backend.Types;
using Liella.Compiler.ILProcessors;
using Liella.TypeAnalysis.Metadata;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler
{
    public struct TypeEntryComparer : IEqualityComparer<ITypeEntry> {
        public bool Equals(ITypeEntry? x, ITypeEntry? y) {
            if(x is TypeInstantiationEntry xInst) {
                if(y is TypeInstantiationEntry yInst) {
                    return xInst.InvariantPart.DefinitionType == yInst.InvariantPart.DefinitionType &&
                        xInst.InvariantPart.TypeArguments.SequenceEqual(yInst.InvariantPart.TypeArguments);
                }
            }
            return x?.Equals(y) ?? false;
        }

        public int GetHashCode([DisallowNull] ITypeEntry obj) {
            return obj.GetHashCode();
        }
    }
    public class LcCompileContext {
        public TypeEnvironment TypeEnv { get; }
        public CodeGenModule Module { get; }
        public CodeGenFactory Backend { get; }
        public CodeGenContext Context { get; }
        public IReadOnlyDictionary<PrimitiveTypeCode, LcPrimitiveTypeInfo> PrimitiveTypes => m_PrimitiveTypes;
        public IReadOnlyDictionary<ITypeEntry, LcTypeInfo> NativeTypeMap => m_NativeTypeMap;
        public IReadOnlyDictionary<IMethodEntry, LcMethodInfo> NativeMethodMap => m_MethodMap;
        public IReadOnlyList<LcTypeInfo> InterfaceRegistry => m_InterfaceRegistry;
        
        protected Dictionary<ITypeEntry, LcTypeInfo> m_NativeTypeMap = new(new TypeEntryComparer());
        protected Dictionary<IMethodEntry, LcMethodInfo> m_MethodMap = new();
        protected Dictionary<PrimitiveTypeCode, LcPrimitiveTypeInfo> m_PrimitiveTypes = new();
        protected List<LcTypeInfo> m_InterfaceRegistry = new();

        public ICGenStructType InterfaceLutType { get; }

        public ILCodeProcessor CodeProcessor { get; }
        public LcCompileContext(TypeEnvironment typeEnv, string projName, string target, string backend = "llvm") {
            TypeEnv = typeEnv;

            var targetSections = target.Split('-');
            var arch = targetSections[0];

            Backend = CodeGenBackends.GetBackend(backend);
            Backend.InitTarget(arch);

            Module = Backend.CreateModule(projName, target);
            Context = Module.Context;


            var typeFactory = Context.TypeFactory;
            InterfaceLutType = typeFactory.CreateStruct([typeFactory.Int32, typeFactory.Int32], "interface_lut");

            CodeProcessor = new() { 
                new ArithmeticEmit(),
                new LocalsEmit(),
                new BitwiseEmit(),
                new BinaryComparsionEmit()
            };
        }
        public int RegisterInterface(LcTypeInfo typeInfo) {
            m_InterfaceRegistry.Add(typeInfo);
            return m_InterfaceRegistry.Count;
        }

        public void StartCompilation() {
            var primitiveImplTypes = TypeEnv.Collector.ActivatedEntity.OfType<PrimitiveTypeEntry>().Select(e => e.GetDetails().DefinitionType);
            foreach(var i in primitiveImplTypes) TypeEnv.Collector.ActivatedEntity.Remove(i);

            foreach(var i in TypeEnv.Collector.ActivatedEntity) {
                if(i is ITypeEntry typeEntry) {
                    if(typeEntry is TypeDefEntry typeDef) {
                        if(typeDef.TypeArguments.Length > 0) continue;
                        m_NativeTypeMap.Add(typeEntry, new LcTypeDefInfo(typeDef, this, Context));
                    }else if(typeEntry is PrimitiveTypeEntry primEntry) {
                        var primitiveImplType = primEntry.GetDetails().DefinitionType;
                        var primitiveType = new LcPrimitiveTypeInfo(primitiveImplType, primEntry, this, Context);

                        m_PrimitiveTypes.Add(primEntry.InvariantPart.TypeCode, primitiveType);
                        m_NativeTypeMap.Add(typeEntry, primitiveType);
                    }else if(typeEntry is PointerTypeEntry pointerEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcPointerTypeInfo(pointerEntry, this, Context));
                    } else if(typeEntry is ReferenceTypeEntry refEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcReferenceTypeInfo(refEntry, this, Context));
                    } else if (typeEntry is TypeInstantiationEntry instEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcTypeInstInfo(instEntry, instEntry.InvariantPart.DefinitionType, this, Context));
                    }
                }
            }

            foreach(var (entry, type) in m_NativeTypeMap) {
                if(type.IsStorageRequired) {
                    type.GetDataStorageTypeEnsureDef();
                    type.GetStaticStorageTypeEnsureDef();
                    type.GetReferenceTypeEnsureDef();
                }
                type.GetInstanceTypeEnsureDef();
                
            }

            foreach(var i in TypeEnv.Collector.ActivatedEntity) {
                if(i is IMethodEntry methodEntry) {
                    var genericContext = (IEntityGenericContextEntry)methodEntry;
                    if(genericContext.MethodArguments.Length > 0 && (methodEntry is not MethodInstantiation)) {
                        continue; // skip generic method definitions
                    }

                    var exactDeclType = methodEntry.DeclType;

                    if(exactDeclType.TypeArguments.Length > 0) {
                        if(methodEntry is MethodInstantiation mi) {
                            exactDeclType = mi.ExactDeclType;
                        } else {
                            continue;
                        }
                    }

                    var typeInfo = NativeTypeMap[exactDeclType];

                    var methodInfo = (methodEntry is MethodInstantiation methodInst) ?
                        new LcMethodInst(typeInfo, methodInst, this, Context)
                        : new LcMethodInfo(typeInfo, methodEntry, this, Context);

                    m_MethodMap.Add(methodEntry, methodInfo);
                    typeInfo.RegisterMethod(methodInfo);

                }
            }

            foreach(var (k,v) in m_MethodMap) {
                v.GetMethodTypeEnsureDef();
            }

            foreach(var (entry, type) in m_NativeTypeMap) {
                if(type.IsStorageRequired) {
                    Console.WriteLine(type.GetVirtualTableType());
                }
            }

            foreach(var (entry, type) in m_NativeTypeMap) {
                if(type.IsStorageRequired) {
                    type.GetVTablePtr();
                }
            }


            foreach(var (k, v) in m_MethodMap) {
                v.GenerateCode();
            }
        }
    }
}
