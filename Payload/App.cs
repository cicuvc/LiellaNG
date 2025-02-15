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
        public static TestStruct Test(in TestStruct x) {
            var z = x;
            z.A = 12;
            return z;
        }
        public static void Main() {
            var g = new TestStruct();
            Test(g);
            //Fuck<uint>.GNN<ulong>();
            var generic = new NestGeneric<short>.Nested<int>();
            generic.NestGeneric<long>();
            //var obj = new object();
            //obj.GetHashCode();
        }
    }
}
