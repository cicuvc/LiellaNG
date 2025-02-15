using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.LLVM {
    internal class App {
        static void Main() {
            var module = LLVMModuleRef.CreateWithName("xxx18");
            var struc = LLVMTypeRef.CreateStruct([LLVMTypeRef.Int32, LLVMTypeRef.Int32], false);

            struc.StructSetBody([LLVMTypeRef.Int64, LLVMTypeRef.Int32], false);

            Console.Write(struc);
        }
    }
}
