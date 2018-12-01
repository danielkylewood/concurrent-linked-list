using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConcurrentLinkedList;
using NUnit.Framework;

namespace LRU.Tests.Unit
{
    public class ConcurrentLinkedListTests
    {
        private ConcurrentLinkedList<dynamic> _linkedList;
            
        [SetUp]
        public void Setup()
        {
            _linkedList = new ConcurrentLinkedList<dynamic>();
        }

        [Test]
        [TestCaseSource(nameof(SingleAddCases))]
        public void When_Adding_A_Single_Node_To_The_List_Then_Entry_Is_Added_Correctly(dynamic nodeValue)
        {
            // Given a value to add
            // When value added to the linked list
            _linkedList.AddFirst(nodeValue);

            // Value exists in linked list and added correctly
            Assert.That(_linkedList.First.Value, Is.EqualTo(nodeValue));
        }

        [Test]
        public async Task When_Adding_Many_Nodes_Concurrently_To_The_List_Then_All_Nodes_Are_Added_Correctly()
        {
            // Given a large number of nodes to add concurrently to the list
            const int numberNodes = 10000;
            var taskList = GenerateAddToLinkedListTasks(numberNodes).ToList();

            // When large number of nodes are added to the list concurrently
            Parallel.ForEach(taskList, task => { task.Start(); });
            await Task.WhenAll(taskList);

            // Then all index values should be represented
            AssertLinkedListIsCycleFree(_linkedList);
            AssertLinkedListNoDuplicatesAndContainsAllNodes(numberNodes, _linkedList);
        }

        private static void AssertLinkedListIsCycleFree(ConcurrentLinkedList<dynamic> linkedList)
        {
            var currentNode = linkedList.First;
            var jumpNode = linkedList.First;
            while (jumpNode != null)
            {
                currentNode = currentNode.Next;
                jumpNode = jumpNode.Next?.Next;
                if (ReferenceEquals(currentNode, jumpNode))
                {
                    Assert.Fail("Cycle detected in linked list.");
                }
            }
        }

        private static void AssertLinkedListNoDuplicatesAndContainsAllNodes(int numberNodes, ConcurrentLinkedList<dynamic> linkedList)
        {
            var currentNode = linkedList.First;
            var hashSet = new HashSet<dynamic>();
            while (currentNode != null)
            {
                if (hashSet.Contains(currentNode.Value))
                {
                    Assert.Fail("Duplicates detected in linked list.");
                }
                hashSet.Add(currentNode.Value);
                currentNode = currentNode.Next;
            }
            Assert.That(hashSet.Count, Is.EqualTo(numberNodes), "Nodes in linked list not accounted for.");
        }

        private IEnumerable<Task> GenerateAddToLinkedListTasks(int numberOfTasks)
        {
            var taskList = new List<Task>();
            for (var i = 1; i <= numberOfTasks; i++)
            {
                var index = i;
                taskList.Add(new Task(() => _linkedList.AddFirst(index)));
            }
            return taskList;
        }

        private static readonly object[] SingleAddCases =
        {
            new object[] { 12 },
            new object[] { "value" },
            new object[] { new LinkedListValue<int, int>(1, 2) },
            new object[] { new LinkedListValue<string, string>("key", "value") },
            new object[] { new LinkedListValue<List<int>, List<int>>(new List<int>(), new List<int>()) }
        };
    }

    internal class LinkedListValue<TKey, TValue>
    {
        public readonly TKey Key;
        public TValue Value;
        public LinkedListValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LinkedListValue<TKey, TValue> asObject))
                return false;
            return asObject.Key.Equals(Key);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TKey>.Default.GetHashCode(Key);
        }
    }
}
