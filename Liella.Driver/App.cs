using Liella.Backend.Components;
using Liella.TypeAnalysis.Metadata;
using Liella.Utils;
using LLVMSharp.Interop;

namespace Liella.Driver {
    class A {
        public static int X;
    }
    class B : A {

    }
    internal class App {
        static unsafe void Main(string[] args) {
            LiLogger.Default.Info("Driver", "Compiler start");

            var ba = CodeGenBackends.GetBackend("llvm");

            var stream = new FileStream("./Payload/Payload.dll", FileMode.Open);
            var icu = new ImageImporter(stream, (asmName) => {
                return new FileStream($"./Payload/{asmName.Name}.dll", FileMode.Open);
            });

            icu.BuildTypeEnvironment();

            LiLogger.Default.Info("Driver", "Type collect complete");

            Console.ReadLine();
        }
    }
}
