using System.Runtime.CompilerServices;

namespace System {
    public readonly ref struct ReadOnlySpan<T> {
        internal readonly ref T _reference;
        private readonly int _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan(void* pointer, int length) {
            _reference = ref Unsafe.As<byte, T>(ref *(byte*)pointer);
            _length = length;
        }

        /// <summary>Creates a new <see cref="ReadOnlySpan{T}"/> of length 1 around the specified reference.</summary>
        /// <param name="reference">A reference to data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(ref readonly T reference) {
            _reference = ref Unsafe.AsRef(in reference);
            _length = 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan(ref T reference, int length) {
            _reference = ref reference;
            _length = length;
        }

    }
}
