using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Payload
{
    public enum XE: int {
        A = 1, B = 2
    }
    public struct TestStruct {
        public int A;
        public int B;
    }
    public class GenericClass<T> {
        public T Value;
        public void GenericFunc() { }
    }
    public interface I0<T,K> {
        T Func0(K x);
    }
    public interface IA<T> :I0<T,int> {

    }
    public class ClassA<T> {
        public T Func0(int x) {
            throw new Exception();
        }
        public void Func1() { }
    }
    public class ClassB<T>: ClassA<T>, IA<T> {
        
    }

    public unsafe class App {
        public static void CallMethod(IA<int> x) {
            x.Func0(12);
        }
        public static void Main() {
            CallMethod(new ClassB<int>());
            //Fuck<uint>.GNN<ulong>();
            //var generic = new NestGeneric<short>.Nested<int>();
            //generic.NestGeneric<long>();
            //var obj = new object();
            //obj.GetHashCode();
        }
    }
}
