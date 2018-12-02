using System.Threading;

namespace ConcurrentLinkedList
{
    public class ConcurrentLinkedList<T>
    {
        public Node<T> First;

        /// <summary>
        /// Attempts to add the specified value to the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        public bool TryAdd(T value)
        {
            var node = new Node<T>(value, (int) NodeState.INS, Thread.CurrentThread.ManagedThreadId);

            Enlist(node);
            var insertionResult = HelpInsert(node, value);
            
            var originalValue = node.AtomicCompareAndExchangeState(insertionResult ? NodeState.DAT : NodeState.INV, NodeState.INS);
            if (originalValue != NodeState.INS)
            {
                HelpRemove(node, value, out _);
                node.State = NodeState.INV;
            }

            return insertionResult;
        }

        /// <summary>
        /// Attempts to remove the specified value from the <see cref="ConcurrentLinkedList{T}"/>.
        /// </summary>
        public bool Remove(T value, out T result)
        {
            var node = new Node<T>(value, NodeState.REM, Thread.CurrentThread.ManagedThreadId);

            Enlist(node);
            var removeResult = HelpRemove(node, value, out result);
            node.State = NodeState.INV;
            return removeResult;
        }

        /// <summary>
        /// Determines whether the <see cref="ConcurrentLinkedList{T}"/> contains the specified key.
        /// </summary>
        public bool Contains(T value)
        {
            var current = First;
            while (current != null)
            {
                if (current.Value.Equals(value))
                {
                    var state = current.State;
                    if (state != NodeState.INV)
                    {
                        return state == NodeState.INS || state == NodeState.DAT;
                    }
                }

                current = current.Next;
            }

            return false;
        }

        private void Enlist(Node<T> node)
        {
            while (true)
            {
                var temporaryFirst = First;
                node.Next = temporaryFirst;
                var originalValue = Interlocked.CompareExchange(ref First, node, temporaryFirst);
                if (ReferenceEquals(originalValue, temporaryFirst))
                {
                    return;
                }
            }
        }

        private static bool HelpInsert(Node<T> node, T value)
        {
            var previous = node;
            var current = previous.Next;
            while (current != null)
            {
                var state = current.State;
                if (state == NodeState.INV)
                {
                    var successor = current.Next;
                    previous.Next = successor;
                    current = successor;
                }
                else if (!current.Value.Equals(value))
                {
                    previous = current;
                    current = current.Next;
                }
                else if (state == NodeState.REM)
                {
                    return true;
                }
                else if (state == NodeState.INS || state == NodeState.DAT)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HelpRemove(Node<T> node, T value, out T result)
        {
            result = default(T);
            var previous = node;
            var current = previous.Next;

            while (current != null)
            {
                var state = current.State;
                if (state == NodeState.INV)
                {
                    var successor = current.Next;
                    previous.Next = successor;
                    current = successor;
                }
                else if (!current.Value.Equals(value))
                {
                    previous = current;
                    current = current.Next;
                }
                else if (state == NodeState.REM)
                {
                    return false;
                }
                else if (state == NodeState.INS)
                {
                    var originalValue = current.AtomicCompareAndExchangeState(NodeState.REM, NodeState.INS);
                    if (originalValue == NodeState.INS)
                    {
                        result = current.Value;
                        current.State = NodeState.INV;
                        return true;
                    }
                }
                else if (state == NodeState.DAT)
                {
                    result = current.Value;
                    current.State = NodeState.INV;
                    return true;
                }
            }

            return false;
        }
    }
}