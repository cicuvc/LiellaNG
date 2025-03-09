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

    public interface IBasic<T> {
        void Func<K>(T x);
    }
    public class CBase {
        public virtual void GVN<T>(T x) { }
        public virtual void NonGVM() {

        }
    }
    public class CDerive : CBase, IBasic<int> {
        public void Func<K>(int x) {
            
        }

        public override void GVN<T>(T x) {

        }
        public override void NonGVM() {

        }
    }


    [NoPruning]
    public unsafe class App {
        public static void CallMethod0(CBase x) {
            x.NonGVM();
        }
        public static void CallMethod(IBasic<int> v) {
            v.Func<short>(12);
            v.Func<long>(12);
        }

        //public static void InitString(ReadOnlySpan<char> s) { }
        public static void Main() {
            var cd = new CDerive();
            CallMethod0(cd);
            CallMethod(cd);

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
