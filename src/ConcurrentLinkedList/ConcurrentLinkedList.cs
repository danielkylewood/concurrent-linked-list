using System.Collections.Concurrent;
using System.Threading;

namespace ConcurrentLinkedList
{
    public class ConcurrentLinkedList<T> : IConcurrentLinkedList<T>
    {
        public Node<T> First => _first;
        
        private int _counter;
        private Node<T> _first;
        private readonly Node<T> _dummy;
        private readonly ConcurrentDictionary<int, ThreadState<T>> _threads;

        public ConcurrentLinkedList()
        {
            _counter = 0;
            _dummy = new Node<T>();
            _threads = new ConcurrentDictionary<int, ThreadState<T>>();
            _first = new Node<T>(default(T), NodeState.REM, -1);
        }

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
            var current = _first;
            while (current != null)
            {
                if (current.Value == null || current.Value.Equals(value))
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
                else if (current.Value != null && !current.Value.Equals(value))
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

        private void Enlist(Node<T> node)
        {
            var phase = Interlocked.Increment(ref _counter);
            var threadState = new ThreadState<T>(phase, true, node);
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            _threads.AddOrUpdate(currentThreadId, threadState, (key, value) => threadState);

            foreach (var threadId in _threads.Keys)
            {
                HelpEnlist(threadId, phase);
            }

            HelpFinish();
        }

        private void HelpEnlist(int threadId, int phase)
        {
            while (IsPending(threadId, phase))
            {
                var current = _first;
                var previous = current.Previous;
                if (current.Equals(_first))
                {
                    if (previous == null)
                    {
                        if (IsPending(threadId, phase))
                        {
                            var node = _threads[threadId].Node;
                            var original = Interlocked.CompareExchange(ref current.Previous, node, null);
                            if (original is null)
                            {
                                HelpFinish();
                                return;
                            }
                        }
                    }
                    else
                    {
                        HelpFinish();
                    }
                }
            }
        }

        private void HelpFinish()
        {
            var current = _first;
            var previous = current.Previous;
            if (previous != null && !previous.IsDummy())
            {
                var threadId = previous.ThreadId;
                var threadState = _threads[threadId];
                if (current.Equals(_first) && previous.Equals(threadState.Node))
                {
                    var currentState = _threads[threadId];
                    var updatedState = new ThreadState<T>(threadState.Phase, false, threadState.Node);
                    _threads.TryUpdate(threadId, updatedState, currentState);
                    previous.Next = current;
                    Interlocked.CompareExchange(ref _first, previous, current);
                    current.Previous = _dummy;
                }
            }
        }

        private bool IsPending(int threadId, int phase)
        {
            var threadState = _threads[threadId];
            return threadState.Pending && threadState.Phase <= phase;
        }
    }
}