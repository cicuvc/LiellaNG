using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Payload
{
    public struct TestStruct {
        public int A;
        public int B;
    }
    public unsafe class App {
        public static TestStruct Test(TestStruct x) {
            x.A = 12;
            return x;
        }
        public static void Main() {
            //Fuck<uint>.GNN<ulong>();
            var generic = new NestGeneric<short>.Nested<int>();
            generic.NestGeneric<long>();
            //var obj = new object();
            //obj.GetHashCode();
        }
    }
}
