using System.Runtime.CompilerServices;

namespace System {
    public readonly ref struct Span<T> {
        internal readonly ref T _reference;
        private readonly int _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span(void* pointer, int length) {
            _reference = ref Unsafe.As<byte, T>(ref *(byte*)pointer);
            _length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span(ref readonly T reference) {
            _reference = ref Unsafe.AsRef(in reference);
            _length = 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(ref T reference, int length) {
            _reference = ref reference;
            _length = length;
        }

    }
}
