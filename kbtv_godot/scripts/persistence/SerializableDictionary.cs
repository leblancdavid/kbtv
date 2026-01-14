using System;
using System.Collections.Generic;
using Godot;
using KBTV.Persistence;

namespace KBTV.Persistence
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        private System.Collections.Generic.Dictionary<TKey, TValue> _dictionary = new System.Collections.Generic.Dictionary<TKey, TValue>();

        public SerializableDictionary()
        {
            _dictionary = new System.Collections.Generic.Dictionary<TKey, TValue>();
        }

        public SerializableDictionary(System.Collections.Generic.Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = new System.Collections.Generic.Dictionary<TKey, TValue>(dictionary);
        }

        // Dictionary-like access
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public int Count => _dictionary.Count;

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);

        public bool Remove(TKey key) => _dictionary.Remove(key);

        public void Clear() => _dictionary.Clear();

        public System.Collections.Generic.Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _dictionary.GetEnumerator();
    }
}