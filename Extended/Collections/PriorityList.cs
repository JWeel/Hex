using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Extended.Collections
{
    /// <summary> Represents a collection which orders elements based on priority. </summary>
    /// <typeparam name="T"> The type of the elements in the collection. </typeparam>
    public class PriorityList<T> : ICollection<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance that uses the default comparer of type <typeparamref name="T"/>. </summary>
        public PriorityList()
            : this(Comparer<T>.Default)
        {
        }

        /// <summary> Initializes a new instance that uses a comparer built by transforming elements of type <typeparamref name="T"/> to <see cref="IComparable"/>. </summary>
        public PriorityList(Func<T, IComparable> func)
            : this(Comparer<T>.Create((x, y) => func(x).CompareTo(func(y))))
        {
        }

        /// <summary> Initializes a new instance that uses a specified comparer. </summary>
        public PriorityList(IComparer<T> prioritizer)
        {
            this.Prioritizer = prioritizer;
            this.Items = new List<T>();
        }

        #endregion

        #region Properties

        /// <summary> The comparer used to sort elements. </summary>
        protected IComparer<T> Prioritizer { get; }

        /// <summary> The elements in the collection. </summary>
        protected List<T> Items { get; }

        #endregion

        #region Methods

        /// <summary> Removes the element with the highest priority from the collection and returns it. </summary>
        /// <remarks> If the collection is empty, an <see cref="InvalidOperationException"/> will be thrown. </remarks>
        public T Pop()
        {
            var item = this.Items.First();
            this.Items.RemoveAt(0);
            return item;
        }

        /// <summary> Removes the element with the highest priority from the collection and returns it. </summary>
        /// <remarks> If the collection is empty, a default instance of type <typeparamref name="T"/> is returned. </remarks>
        public T PopOrDefault()
        {
            if (!this.Items.Any())
                return default;
            return this.Pop();
        }

        #endregion

        #region ICollection Implementation

        /// <summary> The number of items in the collection. </summary>
        public int Count => this.Items.Count;

        /// <summary> Determines if the collection is read-only. </summary>
        public bool IsReadOnly => false;

        /// <summary> Adds an item to the collection, positioned according to priority. </summary>
        public void Add(T item)
        {
            this.Items.Add(item);
            this.Items.Sort(this.Prioritizer);
        }

        /// <summary> Removes all elements from the collection. </summary>
        public void Clear() =>
            this.Items.Clear();

        /// <summary> Determines whether the specified item is in the collection. </summary>
        public bool Contains(T item) =>
            this.Items.Contains(item);

        /// <summary> Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array. </summary>
        public void CopyTo(T[] array, int arrayIndex) =>
            this.Items.CopyTo(array, arrayIndex);

        /// <summary> Removes the first occurence of the specified item from the collection. </summary>
        public bool Remove(T item) =>
            this.Items.Remove(item);

        /// <summary> Returns an enumerator that iterates through the collection. </summary>
        public IEnumerator<T> GetEnumerator() =>
            this.Items.GetEnumerator();

        /// <summary> Returns an enumerator that iterates through the collection. </summary>
        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        #endregion
    }
}