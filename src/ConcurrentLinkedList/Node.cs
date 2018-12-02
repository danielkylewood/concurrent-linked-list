using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("ConcurrentLinkedList.Tests.Unit")]
namespace ConcurrentLinkedList
{
    public class Node<T>
    {
        public T Value;
        public Node<T> Next;
        public Node<T> Previous;

        internal int ThreadId;

        private int _state;
        internal NodeState State
        {
            get => (NodeState)_state;
            set => _state = (int)value;
        }

        internal Node(T value, NodeState state, int threadId)
        {
            Value = value;
            ThreadId = threadId;
            _state = (int)state;
        }

        internal NodeState AtomicCompareAndExchangeState(NodeState value, NodeState compare)
        {
            return (NodeState)Interlocked.CompareExchange(ref _state, (int)value, (int)compare);
        }
    }
}
