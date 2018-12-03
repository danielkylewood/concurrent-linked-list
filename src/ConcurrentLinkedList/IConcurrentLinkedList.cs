namespace ConcurrentLinkedList
{
    public interface IConcurrentLinkedList<T>
    {
        Node<T> First { get; }

        /// <summary>
        /// Attempts to add the specified value to the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        bool TryAdd(T value);

        /// <summary>
        /// Attempts to remove the specified value from the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        bool Remove(T value, out T result);

        /// <summary>
        /// Determines whether the <see cref="ConcurrentLinkedList{T}"/> contains the specified key.
        /// </summary>
        bool Contains(T value);
    }
}
