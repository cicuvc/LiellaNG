using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Liella.TypeAnalysis.Utils
{
    public static class KeyFrozenDictionaryExtension
    {
        public static KeyFrozenDictionary<TKey, TValue> ToKeyFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) where TKey : notnull
        {
            return new(enumerable.ToFrozenDictionary());
        }
        public static KeyFrozenDictionary<TKey, TValue> ToKeyFrozenDictionary<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> selector) where TKey : notnull
        {
            return new(enumerable.ToFrozenDictionary(selector));
        }
        public static KeyFrozenDictionary<TKey, TValue> ToKeyFrozenDictionary<T, TKey, TValue>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector, Func<T, TValue> valueSelector) where TKey : notnull
        {
            return new(enumerable.ToFrozenDictionary(keySelector, valueSelector));
        }
    }
    public struct KeyFrozenDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly FrozenDictionary<TKey, TValue> m_InternalDictionary;
        public KeyFrozenDictionary(FrozenDictionary<TKey, TValue> internalDictionary)
            => m_InternalDictionary = internalDictionary;
        public ref TValue this[TKey key]
        {
            get => ref MemoryMarshal.GetReference(new ReadOnlySpan<TValue>(in m_InternalDictionary[key]));
        }

        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        {
            get => m_InternalDictionary[key];
        }

        public IEnumerable<TKey> Keys => m_InternalDictionary.Keys;

        public IEnumerable<TValue> Values => m_InternalDictionary.Values;

        public int Count => m_InternalDictionary.Count;

        public bool ContainsKey(TKey key) => m_InternalDictionary.ContainsKey(key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_InternalDictionary.GetEnumerator();
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return m_InternalDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_InternalDictionary.GetEnumerator();
        }
    }
}
