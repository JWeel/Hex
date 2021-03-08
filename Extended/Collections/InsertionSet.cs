using System.Collections;
using System.Collections.Generic;

namespace Extended.Collections
{
    /// <summary> Represents a set of elements that preserves insertion ordering. </summary>
    /// <typeparam name="T"> The element type of the set. </typeparam>
    public class InsertionSet<T> : ICollection<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance that uses the default equality comparer of type <typeparamref name="T"/>. </summary>
        public InsertionSet() : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary> Initializes a new instance that uses a specified equality comparer. </summary>
        public InsertionSet(IEqualityComparer<T> comparer)
        {
            this.InternalList = new LinkedList<T>();
            this.InternalMap = new Dictionary<T, LinkedListNode<T>>(comparer);
        }

        #endregion

        #region Properties

        /// <summary> Internal linked list of items. </summary>
        protected LinkedList<T> InternalList { get; }

        /// <summary> Internal map of item to node of <see cref="InternalList"/>. </summary>
        protected IDictionary<T, LinkedListNode<T>> InternalMap { get; }

        /// <summary> Gets the number of elements contained in the set. </summary>
        public int Count => this.InternalMap.Count;

        /// <summary> Gets a value indicating whether the set is read-only (hint: it is not). </summary>
        public bool IsReadOnly => this.InternalMap.IsReadOnly;

        #endregion

        #region Public Methods

        /// <summary> Appends the specified item to the end of the set, but only if it does not already contain it. </summary>
        /// <param name="item"> The item to append to the end of the set. </param>
        /// <returns> <see langword="true"/> if the item was added, <see langword="false"/> if it already existed in the set. </returns>
        public bool Append(T item)
        {
            if (this.InternalMap.ContainsKey(item))
                return false;
            var node = this.InternalList.AddLast(item);
            this.InternalMap.Add(item, node);
            return true;
        }

        /// <summary> Inserts the specified item at the start of the set, but only if it does not already contain it. </summary>
        /// <param name="item"> The item to insert at the start of the set. </param>
        /// <returns> <see langword="true"/> if the item was added, <see langword="false"/> if it already existed in the set. </returns>
        public bool Insert(T item)
        {
            if (this.InternalMap.ContainsKey(item))
                return false;
            var node = this.InternalList.AddFirst(item);
            this.InternalMap.Add(item, node);
            return true;
        }

        #endregion

        #region ICollection Implementation

        /// <summary> Removes an item from the set, if it exists. </summary>
        /// <param name="item"> The item to remove from the set. </param>
        /// <returns> <see langword="true"/> if the item was removed, <see langword="false"/> if it did not exist in the set. </returns>
        public bool Remove(T item)
        {
            if (!this.InternalMap.TryGetValue(item, out var node))
                return false;
            this.InternalMap.Remove(item);
            this.InternalList.Remove(node);
            return true;
        }

        /// <summary> Removes all items from the set. </summary>
        public void Clear()
        {
            this.InternalList.Clear();
            this.InternalMap.Clear();
        }

        /// <summary> Determines whether the set contains the specified item. </summary>
        /// <param name="item"> The item to locate in the set. </param>
        /// <returns> <see langword="true"/> if the item exists in the set, <see langword="false"/> if not. </returns>
        public bool Contains(T item) =>
            this.InternalMap.ContainsKey(item);

        /// <summary> Copies the entire set to a compatible one-dimensional array, starting at the specified index of the target array. </summary>
        /// <param name="array"> The destination array. </param>
        /// <param name="arrayIndex"> The zero-based index in the array at which copying begins. </param>
        public void CopyTo(T[] array, int arrayIndex) =>
            this.InternalList.CopyTo(array, arrayIndex);

        /// <summary> Adds an item to the collection. </summary>
        /// <param name="item"> The item to add. </param>
        void ICollection<T>.Add(T item) =>
            this.Append(item);

        #endregion

        #region IEnumerable Implementation

        /// <summary> Returns an enumerator that iterates through the set. </summary>
        /// <returns> An enumerator for the set. </returns>
        public IEnumerator<T> GetEnumerator() =>
            this.InternalList.GetEnumerator();

        /// <summary> Returns an enumerator that iterates through the set. </summary>
        /// <returns> An enumerator for the set. </returns>
        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        #endregion
    }
}