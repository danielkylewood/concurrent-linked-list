using System;
using System.Threading;

namespace ConcurrentLinkedList
{
    public class ConcurrentLinkedList<T>
    {
        public Node<T> First;

        public ConcurrentLinkedList()
        {

        }

        public bool AddFirst(T value)
        {
            var node = new Node<T>(value, (int) NodeState.INS, Thread.CurrentThread.ManagedThreadId);

            Enlist(node);
            var insertionResult = HelpInsert(node, value);
            
            var originalValue = node.AtomicCompareAndExchangeState(insertionResult ? NodeState.DAT : NodeState.INV, NodeState.INS);
            if (originalValue != NodeState.INS)
            {
                HelpRemove(node, value);
                node.State = NodeState.INV;
            }

            return insertionResult;
        }

        public bool Remove(T value)
        {
            var node = new Node<T>(value, NodeState.REM, Thread.CurrentThread.ManagedThreadId);

            Enlist(node);
            var removeResult = HelpRemove(node, value);
            node.State = NodeState.INV;
            return removeResult;
        }

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

        private static bool HelpRemove(Node<T> node, T value)
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
                    return false;
                }
                else if (state == NodeState.INS)
                {
                    var originalValue = current.AtomicCompareAndExchangeState(NodeState.REM, NodeState.INS);
                    if (originalValue == NodeState.INS)
                    {
                        current.State = NodeState.INV;
                        return true;
                    }
                }
                else if (state == NodeState.DAT)
                {
                    current.State = NodeState.INV;
                    return true;
                }
            }

            return false;
        }
    }
}