using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("ConcurrentLinkedList.Tests.Unit")]
namespace ConcurrentLinkedList
{
    public class Node<T>
    {
        public T Value;
        public Node<T> Next;
        
        private int _state;
        private readonly bool _isDummy;

        internal Node<T> Previous;
        internal int ThreadId;
        internal NodeState State
        {
            get => (NodeState)_state;
            set => _state = (int)value;
        }

        internal Node()
        {
            _isDummy = true;
            Value = default(T);
        }

        internal Node(T value, NodeState state, int threadId)
        {
            Value = value;
            ThreadId = threadId;
            _state = (int)state;
            _isDummy = false;
        }
        
        internal NodeState AtomicCompareAndExchangeState(NodeState value, NodeState compare)
        {
            return (NodeState)Interlocked.CompareExchange(ref _state, (int)value, (int)compare);
        }

        internal bool IsDummy()
        {
            return _isDummy;
        }
    }
}
