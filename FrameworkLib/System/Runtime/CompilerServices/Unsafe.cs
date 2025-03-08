using System;

namespace System.Runtime.CompilerServices {
    public sealed class Unsafe {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static T DereferenceInvariant<T>(T* ptr) where T : unmanaged;
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static T DereferenceInvariantIndex<T>(T* ptr, ulong index) where T : unmanaged;

        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static ulong AsPtr<T>(T obj) where T:class;


        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static ref TTo As<TFrom, TTo>(ref TFrom obj);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static ref TTo AsRef<TTo>(void* obj);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern unsafe static ref T AsRef<T>(scoped ref readonly T obj);
    }
}
