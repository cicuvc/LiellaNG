using System.Runtime.CompilerServices;

namespace System {
    public class Object 
    {
        public virtual int GetHashCode()
        {
            return (int)(Unsafe.AsPtr(this) & 0x7FFFFFFF);
        }
        public virtual bool Equals(object? obj)
        {
            return RuntimeHelpers.Equals(this, obj);
        }
        public virtual object? GetType()
        {
            return null;
        }
        public static unsafe implicit operator void*(object pthis) {
            return (void*)Unsafe.AsPtr(pthis);
        }
    }
}
