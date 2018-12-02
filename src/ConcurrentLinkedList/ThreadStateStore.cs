using System.Threading;

namespace ConcurrentLinkedList
{
    internal class ThreadStateStore<T>
    {
        public ThreadState<T> ThreadState;

        public ThreadStateStore(ThreadState<T> threadState)
        {
            ThreadState = threadState;
        }

        public ThreadState<T> AtomicCompareAndExchangeState(ThreadState<T> value, ThreadState<T> compare)
        {
            return Interlocked.CompareExchange(ref ThreadState, value, compare);
        }
    }
}
