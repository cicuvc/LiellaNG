using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Payload
{
    public enum XE : int {
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
    //public interface I0<T> {
    //    T Func0<W>(W x);
    //}
    //public interface IA<T> : I0<T> {

    //}
    //public class ClassA<T> {
    //    public T Func0<W>(W x) {
    //        throw new Exception();
    //    }
    //    public void Func1() { }
    //}
    //public class ClassB<T> : ClassA<T>, IA<T> {

    //}
    public class CBase<TClass> {
        public virtual void GVN<T>(T x) { }
    }
    public class CDerive<TClass> : CBase<TClass> {
        public override void GVN<T>(T x) {

        }
    }



    public unsafe class App {
        //public static void CallMethod(IA<int> x) {
        //    x.Func0(12);
        //}
        public static void CallMethod<T>(CBase<T> v) {
            v.GVN(12);
        }

        //public static void InitString(ReadOnlySpan<char> s) { }
        public static void Main() {
            CallMethod(new CDerive<GenericClass<GenericClass<int>>>());

            //(new CDerive<short>()).GVN<int>(12);
            //CallMethod(new ClassB<int>());
            //Fuck<uint>.GNN<ulong>();
            //var generic = new NestGeneric<short>.Nested<int>();
            //generic.NestGeneric<long>();
            //var obj = new object();
            //obj.GetHashCode();
        }
    }
}
