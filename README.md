## Concurrent Linked List

A lock-free thread-safe linked list implementation in C#.

Algorithms are based on the work presented in "Zhang, K et al. Practical Non-blocking Unordered Lists".

While the linked list is thread-safe and lock-free, it is not wait-free. Some threads may starve due to contention. Will fix in the future.

