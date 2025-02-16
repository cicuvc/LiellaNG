using Liella.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Components {
    public static class CodeGenBackends {
        public static Dictionary<string, (string asmPath, string type)> BackendRegistry { get; } = new() {
            { "llvm", ("Liella.Backend.LLVM.dll","Liella.Backend.LLVM.LLVMCodeGenFactory") }
        };
        public static Dictionary<string, CodeGenFactory> BackendCache { get; } = new();
        public static CodeGenFactory GetBackend(string name) {
            if(!BackendCache.TryGetValue(name, out var backend)) {
                if(!BackendRegistry.ContainsKey(name)) {
                    LiLogger.Default.Error("CodeGenBackends", $"Unknown backend '{name}'", null);
                    throw new KeyNotFoundException($"Unknown backend '{name}'");
                }
                var (asmPath, type) = BackendRegistry[name];
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(asmPath));
                var typeInfo = assembly.GetType(type);
                var factory = (CodeGenFactory)typeInfo!.GetConstructor([])!.Invoke([]);

                BackendCache.Add(name, backend = factory);
            }

            return backend;
        }
    }
    public abstract class CodeGenFactory {
        public abstract void InitTarget(string targetName);
        public abstract CodeGenCompiler CreateCompiler();
        public abstract CodeGenCompileOptions CreateCompileOptions();
        public abstract CodeGenTargetInfo CreateTargetInfo();
        public abstract CodeGenModule CreateModule(string name, string target);
    }
}
