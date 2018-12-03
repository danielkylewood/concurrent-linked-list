using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ConcurrentLinkedList.Tests.Unit
{
    public class ConcurrentLinkedListTests : UnitTestBase
    {
        private const int _numNodesToTest = 10000;
        private const int _initialNodesInList = 1;
        private IConcurrentLinkedList<dynamic> _linkedList;

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
            var result = _linkedList.TryAdd(nodeValue);

            // Value exists in linked list and added correctly
            Assert.That(result, Is.True);
            Assert.That(_linkedList.First.Value, Is.EqualTo(nodeValue));
        }

        [Test]
        public async Task When_Adding_Many_Nodes_Concurrently_To_The_List_Then_All_Nodes_Are_Added_Correctly()
        {
            // Given a large number of nodes to add concurrently to the list
            const int numberNodes = _numNodesToTest;
            var taskList = GenerateTasks(numberNodes, 1, value => _linkedList.TryAdd(value)).ToList();

            // When large number of nodes are added to the list concurrently
            Parallel.ForEach(taskList, task => { task.Start(); });
            await Task.WhenAll(taskList);

            // Then all index values should be represented
            AssertTaskListResults(taskList);
            AssertLinkedListHasNoCycles(_linkedList);
            AssertLinkedListHasNoDuplicate(_linkedList);
            AssertLinkedListContainsAllNodes(numberNodes + _initialNodesInList, _linkedList);
        }

        [Test]
        [TestCaseSource(nameof(SingleRemoveCases))]
        public void When_Adding_Then_Removing_A_Single_Node_To_The_List_Then_Entry_Is_Removed_Correctly(dynamic nodeValue)
        {
            // Given a value to add then remove
            // When value added to the linked list
            _linkedList.TryAdd(nodeValue);

            // And the result is removed
            var result = _linkedList.Remove(nodeValue, out dynamic nodeValueRetrieved);

            // Value exists in linked list and added correctly
            Assert.That(result, Is.True);
            Assert.That(nodeValueRetrieved, Is.EqualTo(nodeValue));
            Assert.That(_linkedList.First.Value, Is.EqualTo(nodeValue));
        }

        [Test]
        public async Task When_Removing_Many_Nodes_Concurrently_From_The_List_Then_All_Nodes_Are_Removed_Correctly()
        {
            // Given a large number of nodes to add and remove concurrently to the list
            const int startIndex = 1;
            const int numberNodes = _numNodesToTest;
            var addTaskList = GenerateTasks(numberNodes, startIndex, value => _linkedList.TryAdd(value)).ToList();
            var removeTaskList = GenerateTasks(numberNodes, startIndex, value => _linkedList.Remove(value, out var _)).ToList();

            // When large number of nodes are added to the list concurrently
            Parallel.ForEach(addTaskList, task => { task.Start(); });
            await Task.WhenAll(addTaskList);

            // And a large number of nodes are removed from the list concurrently
            Parallel.ForEach(removeTaskList, task => { task.Start(); });
            await Task.WhenAll(removeTaskList);

            // Then there should be no active state nodes left in the list
            AssertTaskListResults(addTaskList);
            AssertTaskListResults(removeTaskList);
            AssertLinkedListOnlyContainsInvalidStateNodes(_linkedList);
        }

        [Test]
        public async Task When_Adding_And_Removing_At_The_Same_Time_Then_No_Add_Or_Remove_Should_Fail()
        {
            // Given a large number of nodes to add and remove concurrently to the list
            const int startIndex = 1;
            const int numberNodes = _numNodesToTest;
            var addTaskList = GenerateTasks(numberNodes, startIndex, value => _linkedList.TryAdd(new LinkedListValue<int, int>(value, value))).ToList();
            var removeTaskList = GenerateTasks(numberNodes, startIndex, value => _linkedList.Remove(new LinkedListValue<int, int>(value, value), out var _)).ToList();
            removeTaskList.AddRange(GenerateTasks(numberNodes, startIndex + numberNodes + 1, value => _linkedList.TryAdd(new LinkedListValue<int, int>(value, value))));

            // When large number of nodes are added to the list concurrently
            Parallel.ForEach(addTaskList, task => { task.Start(); });
            await Task.WhenAll(addTaskList);

            // And a large number of nodes are added and removed from the list concurrently
            Parallel.ForEach(removeTaskList, task => { task.Start(); });
            await Task.WhenAll(removeTaskList);

            // Then all tasks should have been successful and there should be a certain number of valid nodes
            AssertTaskListResults(addTaskList);
            AssertTaskListResults(removeTaskList);
            AssertLinkedListContainsNumberOfValidNodes(numberNodes + _initialNodesInList, _linkedList);
        }
       
        [Test]
        public void When_Node_Exists_Checking_Contains_Should_Return_True()
        {
            // Given a value to add
            const string valueToAdd = "ValueToAdd";

            // And the value is added
            var result = _linkedList.TryAdd(valueToAdd);
            Assert.That(result, Is.True);

            // When we check if the value is there
            var contains = _linkedList.Contains(valueToAdd);

            // The result should be true
            Assert.That(contains, Is.True);
        }

        private static readonly object[] SingleAddCases =
        {
            new object[] { 12 },
            new object[] { "value" },
            new object[] { new LinkedListValue<int, int>(1, 2) },
            new object[] { new LinkedListValue<string, string>("key", "value") },
            new object[] { new LinkedListValue<List<int>, List<int>>(new List<int>(), new List<int>()) }
        };

        private static readonly object[] SingleRemoveCases =
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
