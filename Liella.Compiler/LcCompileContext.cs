using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.TypeAnalysis.Metadata;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler
{
    public class LcCompileContext {
        public TypeEnvironment TypeEnv { get; }
        public CodeGenModule Module { get; }
        public CodeGenFactory Backend { get; }
        public CodeGenContext Context { get; }
        public IReadOnlyDictionary<ITypeEntry, LcTypeInfo> NativeTypeMap => m_NativeTypeMap;
        protected Dictionary<ITypeEntry, LcTypeInfo> m_NativeTypeMap = new();

        public LcCompileContext(TypeEnvironment typeEnv, string projName, string target, string backend = "llvm") {
            TypeEnv = typeEnv;

            var targetSections = target.Split('-');
            var arch = targetSections[0];

            Backend = CodeGenBackends.GetBackend(backend);
            Backend.InitTarget(arch);

            Module = Backend.CreateModule(projName, target);
            Context = Module.Context;
        }
        public void StartCompilation() {
            foreach(var i in TypeEnv.Collector.ActivatedEntity) {
                if(i is ITypeEntry typeEntry) {
                    if(typeEntry is TypeDefEntry typeDef) {
                        m_NativeTypeMap.Add(typeEntry, new LcTypeDefInfo(typeDef, this, Context));
                    }else if(typeEntry is PrimitiveTypeEntry primEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcPrimitiveTypeInfo(primEntry.GetDetails().DefinitionType, primEntry, this, Context));
                    }else if(typeEntry is PointerTypeEntry pointerEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcPointerTypeInfo(pointerEntry, this, Context));
                    } else if(typeEntry is ReferenceTypeEntry refEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcPointerTypeInfo(refEntry, this, Context));
                    } else if (typeEntry is TypeInstantiationEntry instEntry) {
                        m_NativeTypeMap.Add(typeEntry, new LcTypeInstInfo(instEntry, instEntry.InvariantPart.DefinitionType, this, Context));
                    }
                }
            }

            foreach(var (entry, type) in m_NativeTypeMap) {
                if(type.IsStorageRequired) {
                    type.GetDataStorageTypeEnsureDef();
                    type.GetStaticStorageTypeEnsureDef();
                }
                Console.WriteLine($"{type.Entry}: {type.GetInstanceTypeEnsureDef()}");
                
            }
        }
    }
}
