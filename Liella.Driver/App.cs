using Liella.TypeAnalysis.Metadata;
using LLVMSharp.Interop;

namespace Liella.Driver {
    class A {
        public static int X;
    }
    class B : A {

    }
    internal class App {
        static unsafe void Main(string[] args) {
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, []);
            var jitModule = LLVMModuleRef.CreateWithName("eval");
            var builder = jitModule.Context.CreateBuilder();
            var func = jitModule.AddFunction("eval_0", funcType);
            builder.PositionAtEnd(func.AppendBasicBlock("entry"));

            var structType = LLVMTypeRef.CreateStruct([
                LLVMTypeRef.Double, 
                LLVMTypeRef.Int8, LLVMTypeRef.Int8, LLVMTypeRef.Int8,
                ], false);
            builder.BuildRet(structType.SizeOf);

            
            var jitEE = jitModule.CreateExecutionEngine();
            var fnPtr = jitEE.RecompileAndRelinkFunction(func);
            var value = jitEE.RunFunction(func, []);
            Console.WriteLine(LLVM.GenericValueToInt(value, 1));
            LLVM.DisposeGenericValue(value);



            Environment.Exit(0);

            var stream = new FileStream("./Payload/Payload.dll", FileMode.Open);
            var icu = new ImageImporter(stream, (asmName) => {
                return new FileStream($"./Payload/{asmName.Name}.dll", FileMode.Open);
            });

            icu.BuildTypeEnvironment();


            Console.ReadLine();
        }
    }
}
