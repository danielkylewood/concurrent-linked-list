namespace ConcurrentLinkedList
{
    internal class ThreadState<T>
    {
        public int Phase;
        public bool Pending;
        public Node<T> Node;

        public ThreadState(int phase, bool pending, Node<T> node)
        {
            Phase = phase;
            Pending = pending;
            Node = node;
        }
    }
}
