using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace Ded.Wordox
{
    public class Chrono
    {
        #region Fields
        private readonly DateTime start = DateTime.Now;
        #endregion
        public TimeSpan Elapsed { get { return DateTime.Now - start; } }
    }
    class AssemblyResources
    {
        #region Fields
        private readonly Assembly assembly;
        private readonly string resourcePath;
        #endregion
        public AssemblyResources(Type type, string resourcePath)
            : this(type.Assembly, resourcePath)
        {
        }
        public AssemblyResources(Assembly assembly, string resourcePath)
        {
            this.assembly = assembly;
            this.resourcePath = resourcePath;
        }
        public string GetContent(string name)
        {
            using (var stream = assembly.GetManifestResourceStream(resourcePath + name))
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd();
        }
    }
    class ConstantList<T> : IList<T>
    {
        #region Fields
        private readonly ReadOnlyCollection<T> wrapped;
        #endregion
        public ConstantList()
            : this(new List<T>())
        {
        }
        public ConstantList(T item)
            : this(new List<T> { item })
        {
        }
        public ConstantList(IList<T> seq)
        {
            wrapped = new ReadOnlyCollection<T>(seq);
        }
        public int IndexOf(T item)
        {
            return wrapped.IndexOf(item);
        }
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
        public T this[int index]
        {
            get { return wrapped[index]; }
            set { throw new NotSupportedException(); }
        }
        public void Add(T item)
        {
            throw new NotSupportedException();
        }
        public void Clear()
        {
            throw new NotSupportedException();
        }
        public bool Contains(T item)
        {
            return wrapped.Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            wrapped.CopyTo(array, arrayIndex);
        }
        public int Count
        {
            get { return wrapped.Count; }
        }
        public bool IsReadOnly
        {
            get { return true; }
        }
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return wrapped.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    class ConstantSet<T> : ISet<T>
    {
        #region Fields
        private readonly HashSet<T> wrapped;
        #endregion
        public ConstantSet()
            : this(new T[0])
        {
        }
        public ConstantSet(T item)
            : this(new[] { item })
        {
        }
        public ConstantSet(IEnumerable<T> items)
        {
            wrapped = new HashSet<T>(items);
        }
        public bool Add(T item)
        {
            throw new NotSupportedException();
        }
        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }
        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return wrapped.IsProperSubsetOf(other);
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return wrapped.IsProperSupersetOf(other);
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return wrapped.IsSubsetOf(other);
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return wrapped.IsSupersetOf(other);
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            return wrapped.Overlaps(other);
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            return wrapped.SetEquals(other);
        }
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }
        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }
        public void Clear()
        {
            throw new NotSupportedException();
        }
        public bool Contains(T item)
        {
            return wrapped.Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            wrapped.CopyTo(array, arrayIndex);
        }
        public int Count
        {
            get { return wrapped.Count; }
        }
        public bool IsReadOnly
        {
            get { return true; }
        }
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return wrapped.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    class ConstantDictionary<K, V> : IDictionary<K, V>
    {
        #region Fields
        private readonly ReadOnlyDictionary<K, V> wrapped;
        #endregion
        public ConstantDictionary()
            : this(new Dictionary<K, V>())
        {
        }
        public ConstantDictionary(IDictionary<K, V> map)
        {
            wrapped = new ReadOnlyDictionary<K, V>(map);
        }
        public void Add(K key, V value)
        {
            throw new NotSupportedException();
        }
        public bool ContainsKey(K key)
        {
            return wrapped.ContainsKey(key);
        }
        public ICollection<K> Keys
        {
            get { return wrapped.Keys; }
        }
        public bool Remove(K key)
        {
            throw new NotSupportedException();
        }
        public bool TryGetValue(K key, out V value)
        {
            return wrapped.TryGetValue(key, out value);
        }
        public ICollection<V> Values
        {
            get { return wrapped.Values; }
        }
        public V this[K key]
        {
            get { return wrapped[key]; }
            set { throw new NotSupportedException(); }
        }
        public void Add(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }
        public void Clear()
        {
            throw new NotSupportedException();
        }
        public bool Contains(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        public int Count
        {
            get { return wrapped.Count; }
        }
        public bool IsReadOnly
        {
            get { return true; }
        }
        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return wrapped.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    static class ISetExtensions
    {
        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> items)
        {
            foreach (T item in items)
                set.Add(item);
        }
        public static ConstantSet<T> ToConstant<T>(this ISet<T> set)
        {
            return set as ConstantSet<T> ?? new ConstantSet<T>(set);
        }
    }
    static class IListExtensions
    {
        public static ConstantList<T> ToConstant<T>(this IList<T> list)
        {
            return list as ConstantList<T> ?? new ConstantList<T>(list);
        }
    }
    static class StringExtensions
    {
        public static string Sort(this string s)
        {
            var letters = new List<char>(s);
            letters.Sort();
            return new string(letters.ToArray());
        }
        public static bool Contains(this string s, string value, StringComparison comparison)
        {
            return s.IndexOf(value, comparison) > -1;
        }
    }
    static class IDictionaryExtensions
    {
        public static void AddRange<K, V>(this IDictionary<K, V> map, IDictionary<K, V> other)
        {
            foreach (KeyValuePair<K, V> kv in other)
                map.Add(kv.Key, kv.Value);
        }
        public static ConstantDictionary<K, V> ToConstant<K, V>(this IDictionary<K, V> map)
        {
            return map as ConstantDictionary<K, V> ?? new ConstantDictionary<K, V>(map);
        }
    }
    class RandomValues
    {
        #region Fields
        private readonly Random random;
        #endregion
        public RandomValues()
        {
            random = new Random();
        }
        public bool Bool
        {
            get { return GetInt(2) == 0; }
        }
        public int GetInt(int range)
        {
            return random.Next() % range;
        }
    }
}
