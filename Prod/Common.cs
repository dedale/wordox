using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace Ded.Wordox
{
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
    static class ISetExtensions
    {
        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> items)
        {
            foreach (T item in items)
                set.Add(item);
        }
        public static ConstantSet<T> ToConstant<T>(this ISet<T> set)
        {
            if (set is ConstantSet<T>)
                return (ConstantSet<T>)set;
            return new ConstantSet<T>(set);
        }
    }
}
