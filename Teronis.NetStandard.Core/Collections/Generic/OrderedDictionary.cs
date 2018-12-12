﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Teronis.NetStandard.Collections.Specialized;
using Teronis.NetStandard.Extensions;

namespace Teronis.NetStandard.Collections.Generic
{
    public class ComparerByHandler<T> : Comparer<T>
    {
        private readonly Comparison<T> compareHandler;

        public ComparerByHandler(Comparison<T> compareHandler) => this.compareHandler = compareHandler ?? throw new ArgumentNullException(nameof(compareHandler));

        public override int Compare(T arg1, T arg2) => compareHandler(arg1, arg2);
    }

    public class TKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem>
    {
        private const string DELEGATE_NULL_EXCEPTION_MESSAGE = "Delegate passed cannot be null";
        private Func<TItem, TKey> _getKeyForItemFunction;

        public TKeyedCollection(Func<TItem, TKey> getKeyForItemFunction) : base()
            => _getKeyForItemFunction = getKeyForItemFunction ?? throw new ArgumentNullException(DELEGATE_NULL_EXCEPTION_MESSAGE);

        public TKeyedCollection(Func<TItem, TKey> getKeyForItemDelegate, IEqualityComparer<TKey> comparer) : base(comparer)
            => _getKeyForItemFunction = getKeyForItemDelegate ?? throw new ArgumentNullException(DELEGATE_NULL_EXCEPTION_MESSAGE);

        protected override TKey GetKeyForItem(TItem item) => _getKeyForItemFunction(item);

        public void SortByKeys() => SortByKeys(Comparer<TKey>.Default);
        public void SortByKeys(IComparer<TKey> keyComparer) => new ComparerByHandler<TItem>((x, y) => keyComparer.Compare(GetKeyForItem(x), GetKeyForItem(y)));
        public void SortByKeys(Comparison<TKey> keyComparison) => Sort(new ComparerByHandler<TItem>((x, y) => keyComparison(GetKeyForItem(x), GetKeyForItem(y))));
        public void Sort() => Sort(Comparer<TItem>.Default);
        public void Sort(Comparison<TItem> comparison) => Sort(new ComparerByHandler<TItem>((x, y) => comparison(x, y)));
        public void Sort(IComparer<TItem> comparer) => (Items as List<TItem>)?.Sort(comparer);
    }

    public class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator, IDisposable
    {
        readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
        public void Dispose() { enumerator.Dispose(); }
        public DictionaryEnumerator(IDictionary<TKey, TValue> value)
        {
            this.enumerator = value.GetEnumerator();
        }
        public void Reset() { enumerator.Reset(); }
        public bool MoveNext() { return enumerator.MoveNext(); }
        public DictionaryEntry Entry {
            get {
                var pair = enumerator.Current;
                return new DictionaryEntry(pair.Key, pair.Value);
            }
        }
        public object Key { get { return enumerator.Current.Key; } }
        public object Value { get { return enumerator.Current.Value; } }
        public object Current { get { return Entry; } }
    }

    /// <summary>
    /// A dictionary object that allows rapid hash lookups using keys, but also
    /// maintains the key insertion order so that values can be retrieved by
    /// key index.
    /// </summary>
    /// <remarks>
    /// Similar to the way a DataColumn is indexed by column position and by column name, this
    /// advanced dictionary construct allows for a very natural and robust handling of indexed
    /// structured data.
    /// </remarks>
    public class OrderedDictionary<K, V> : IOrderedDictionary<K, V>
    {
        /* Fields/Properties */

        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
        IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

        private TKeyedCollection<K, KeyValuePair<K, V>> _keyedCollection;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the value to get or set.</param>
        public V this[K key] {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <param name="index">The index of the value to get or set.</param>
        public KeyValuePair<K, V> this[int index] {
            get => GetItem(index);
            set => SetItem(index, value.Value);
        }

        /// <summary>
        /// Gets the number of items in the dictionary
        /// </summary>
        public int Count => _keyedCollection.Count;

        /// <summary>
        /// Gets all the keys in the ordered dictionary in their proper order.
        /// </summary>
        public ICollection<K> Keys => _keyedCollection.Select(x => x.Key).ToList();

        /// <summary>
        /// Gets all the values in the ordered dictionary in their proper order.
        /// </summary>
        public ICollection<V> Values => _keyedCollection.Select(x => x.Value).ToList();

        /// <summary>
        /// Gets the key comparer for this dictionary
        /// </summary>
        public IEqualityComparer<K> Comparer { get; private set; }

        /* Constructors */

        public OrderedDictionary() => Initialize();
        public OrderedDictionary(IEqualityComparer<K> comparer) => Initialize(comparer);

        public OrderedDictionary(IOrderedDictionary<K, V> dictionary)
        {
            Initialize();

            foreach (var pair in dictionary) {
                _keyedCollection.Add(pair);
            }
        }

        public OrderedDictionary(IOrderedDictionary<K, V> dictionary, IEqualityComparer<K> comparer)
        {
            Initialize(comparer);

            foreach (var pair in dictionary) {
                _keyedCollection.Add(pair);
            }
        }

        public OrderedDictionary(IEnumerable<KeyValuePair<K, V>> items)
        {
            Initialize();

            foreach (var pair in items) {
                _keyedCollection.Add(pair);
            }
        }

        public OrderedDictionary(IEnumerable<KeyValuePair<K, V>> items, IEqualityComparer<K> comparer)
        {
            Initialize(comparer);

            foreach (var pair in items) {
                _keyedCollection.Add(pair);
            }
        }

        /* Methods */

        private void Initialize(IEqualityComparer<K> comparer = null)
        {
            Comparer = comparer;

            if (comparer != null) {
                _keyedCollection = new TKeyedCollection<K, KeyValuePair<K, V>>(x => x.Key, comparer);
            } else {
                _keyedCollection = new TKeyedCollection<K, KeyValuePair<K, V>>(x => x.Key);
            }
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.  The value can be null for reference types.</param>
        public void Add(K key, V value) => _keyedCollection.Add(new KeyValuePair<K, V>(key, value));

        //public void Add<KeyableV>(KeyableV keyableVal) where KeyableV: V, IKeyable<K>
        //{
        //    Add(keyableVal.KeyItem, keyableVal);
        //}

        //public void TryAdd<KeyableV>(KeyableV keyableVal) where KeyableV : V, IKeyable<K>
        //{
        //    return TryAdd(keyableVal.KeyItem, keyableVal);
        //}

        /// <summary>
        /// Removes all keys and values from this object.
        /// </summary>
        public void Clear() => _keyedCollection.Clear();

        public void Insert(int index, KeyValuePair<K, V> pair) => _keyedCollection.Insert(index, pair);

        /// <summary>
        /// Inserts a new key-value pair at the index specified.
        /// </summary>
        /// <param name="index">The insertion index.  This value must be between 0 and the count of items in this object.</param>
        /// <param name="key">A unique key for the element to add</param>
        /// <param name="value">The value of the element to add.  Can be null for reference types.</param>
        public void Insert(int index, K key, V value) => Insert(index, key, value);

        /// <summary>
        /// Gets the index of the key specified.
        /// </summary>
        /// <param name="key">The key whose index will be located</param>
        /// <returns>Returns the index of the key specified if found.  Returns -1 if the key could not be located.</returns>
        public int IndexOf(K key)
        {
            if (_keyedCollection.Contains(key))
                return _keyedCollection.IndexOf(_keyedCollection[key]);
            else
                return -1;
        }

        /// <summary>
        /// Determines whether this object contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate in this object.</param>
        /// <returns>True if the value is found.  False otherwise.</returns>
        public bool ContainsValue(V value) => Values.Contains(value);

        /// <summary>
        /// Determines whether this object contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate in this object.</param>
        /// <param name="comparer">The equality comparer used to locate the specified value in this object.</param>
        /// <returns>True if the value is found.  False otherwise.</returns>
        public bool ContainsValue(V value, IEqualityComparer<V> comparer) => Values.Contains(value, comparer);

        /// <summary>
        /// Determines whether this object contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in this object.</param>
        /// <returns>True if the key is found.  False otherwise.</returns>
        public bool ContainsKey(K key) => _keyedCollection.Contains(key);

        /// <summary>
        /// Returns the KeyValuePair at the index specified.
        /// </summary>
        /// <param name="index">The index of the KeyValuePair desired</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index specified does not refer to a KeyValuePair in this object
        /// </exception>
        public KeyValuePair<K, V> GetItem(int index)
        {
            if (index < 0 || index >= _keyedCollection.Count)
                throw new ArgumentException("The index was outside the bounds of the dictionary: {0}".Format(index));

            return _keyedCollection[index];
        }

        /// <summary>
        /// Sets the value at the index specified.
        /// </summary>
        /// <param name="index">The index of the value desired</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the index specified does not refer to a KeyValuePair in this object
        /// </exception>
        public void SetItem(int index, V value)
        {
            if (index < 0 || index >= _keyedCollection.Count)
                throw new ArgumentException("The index is outside the bounds of the dictionary: {0}".Format(index));
            _keyedCollection[index] = new KeyValuePair<K, V>(_keyedCollection[index].Key, value); ;
        }

        /// <summary>
        /// Returns an enumerator that iterates through all the KeyValuePairs in this object.
        /// </summary>
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _keyedCollection.GetEnumerator();

        /// <summary>
        /// Removes the key-value pair for the specified key.
        /// </summary>
        /// <param name="key">The key to remove from the dictionary.</param>
        /// <returns>True if the item specified existed and the removal was successful.  False otherwise.</returns>
        public bool Remove(K key) => _keyedCollection.Remove(key);

        /// <summary>
        /// Removes the key-value pair at the specified index.
        /// </summary>
        /// <param name="index">The index of the key-value pair to remove from the dictionary.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _keyedCollection.Count)
                throw new ArgumentException("The index was outside the bounds of the dictionary: {0}".Format(index));
            _keyedCollection.RemoveAt(index);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the value to get.</param>
        public V GetValue(K key)
        {
            if (_keyedCollection.Contains(key) == false)
                throw new ArgumentException("The given key is not present in the dictionary: {0}".Format(key));
            var kvp = _keyedCollection[key];
            return kvp.Value;
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the value to set.</param>
        /// <param name="value">The the value to set.</param>
        public void SetValue(K key, V value)
        {
            var kvp = new KeyValuePair<K, V>(key, value);
            var idx = IndexOf(key);

            if (idx > -1) {
                _keyedCollection[idx] = kvp;
            } else {
                _keyedCollection.Add(kvp);
            }
        }

        /// <summary>
        /// Tries to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the desired element.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key if
        /// that key was found.  Otherwise it will contain the default value for parameter's type.
        /// This parameter should be provided uninitialized.
        /// </param>
        /// <returns>True if the value was found.  False otherwise.</returns>
        /// <remarks></remarks>
        public bool TryGetValue(K key, out V value)
        {
            if (_keyedCollection.Contains(key)) {
                value = _keyedCollection[key].Value;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        public ReadOnlyDictionary<K, V> AsReadOnly() => new ReadOnlyDictionary<K, V>(this);

        /* Sorting */
        public void SortKeys() => _keyedCollection.SortByKeys();
        public void SortKeys(IComparer<K> comparer) => _keyedCollection.SortByKeys(comparer);
        public void SortKeys(Comparison<K> comparison) => _keyedCollection.SortByKeys(comparison);

        public void SortValues()
        {
            var comparer = Comparer<V>.Default;
            SortValues(comparer);
        }

        public void SortValues(IComparer<V> comparer) => _keyedCollection.Sort((x, y) => comparer.Compare(x.Value, y.Value));
        public void SortValues(Comparison<V> comparison) => _keyedCollection.Sort((x, y) => comparison(x.Value, y.Value));

        /* IDictionary<TKey, TValue> */

        void IDictionary<K, V>.Add(K key, V value) => Add(key, value);
        bool IDictionary<K, V>.ContainsKey(K key) => ContainsKey(key);
        ICollection<K> IDictionary<K, V>.Keys => Keys;
        bool IDictionary<K, V>.Remove(K key) => Remove(key);
        bool IDictionary<K, V>.TryGetValue(K key, out V value) => TryGetValue(key, out value);
        ICollection<V> IDictionary<K, V>.Values => Values;

        V IDictionary<K, V>.this[K key] {
            get => this[key];
            set => this[key] = value;
        }

        /* ICollection<KeyValuePair<TKey, TValue>> */

        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item) => _keyedCollection.Add(item);
        void ICollection<KeyValuePair<K, V>>.Clear() => _keyedCollection.Clear();
        bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item) => _keyedCollection.Contains(item);
        void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) => _keyedCollection.CopyTo(array, arrayIndex);
        int ICollection<KeyValuePair<K, V>>.Count => _keyedCollection.Count;
        bool ICollection<KeyValuePair<K, V>>.IsReadOnly => false;
        bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item) => _keyedCollection.Remove(item);

        /* IEnumerable<KeyValuePair<TKey, TValue>> */

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();

        /* IEnumerable */

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /* IOrderedDictionary */

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator() => new DictionaryEnumerator<K, V>(this);
        void IOrderedDictionary.Insert(int index, object key, object value) => Insert(index, (K)key, (V)value);
        void IOrderedDictionary.RemoveAt(int index) => RemoveAt(index);

        object IOrderedDictionary.this[int index] {
            get => this[index];
            set => this[index] = (KeyValuePair<K, V>)value;
        }

        /* IDictionary */

        void IDictionary.Add(object key, object value) => Add((K)key, (V)value);
        void IDictionary.Clear() => Clear();
        bool IDictionary.Contains(object key) => _keyedCollection.Contains((K)key);
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator<K, V>(this);
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => (ICollection)Keys;
        void IDictionary.Remove(object key) => Remove((K)key);
        ICollection IDictionary.Values => (ICollection)Values;

        object IDictionary.this[object key] {
            get => this[(K)key];
            set => this[(K)key] = (V)value;
        }

        /* ICollection */

        void ICollection.CopyTo(Array array, int index) => ((ICollection)_keyedCollection).CopyTo(array, index);
        int ICollection.Count => ((ICollection)_keyedCollection).Count;
        bool ICollection.IsSynchronized => ((ICollection)_keyedCollection).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)_keyedCollection).SyncRoot;
    }
}
