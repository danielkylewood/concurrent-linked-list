using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ConcurrentLinkedList.Tests.Unit
{
    public class UnitTestBase
    {
        protected static void AssertTaskListResults(IEnumerable<Task<bool>> taskList)
        {
            foreach (var task in taskList)
            {
                if (task.Result != true)
                {
                    Assert.Fail("One or more tasks failed.");
                }
            }
        }

        protected static void AssertLinkedListHasNoCycles(IConcurrentLinkedList<dynamic> linkedList)
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

        protected static void AssertLinkedListOnlyContainsInvalidStateNodes(IConcurrentLinkedList<dynamic> linkedList)
        {
            var numRemNodes = 0;
            var currentNode = linkedList.First;
            while (currentNode != null)
            {
                if (currentNode.State != NodeState.INV)
                {
                    if (currentNode.State != NodeState.REM)
                    {
                        Assert.Fail("Valid node detected in list.");
                    }
                    else
                    {
                        numRemNodes++;
                    }
                }
                currentNode = currentNode.Next;
            }
            Assert.That(numRemNodes, Is.LessThanOrEqualTo(1));
        }

        protected static void AssertLinkedListHasNoDuplicate(IConcurrentLinkedList<dynamic> linkedList)
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
        }

        protected static void AssertLinkedListContainsAllNodes(int numberNodes, IConcurrentLinkedList<dynamic> linkedList)
        {
            var nodeCount = 0;
            var currentNode = linkedList.First;
            while (currentNode != null)
            {
                nodeCount++;
                currentNode = currentNode.Next;
            }

            Assert.That(nodeCount, Is.EqualTo(numberNodes), "Nodes in linked list not accounted for.");
        }

        protected static void AssertLinkedListContainsNumberOfValidNodes(int numberValidNodes, IConcurrentLinkedList<dynamic> linkedList)
        {
            var nodeCount = 0;
            var currentNode = linkedList.First;
            while (currentNode != null)
            {
                if (currentNode.State != NodeState.INV)
                {
                    nodeCount++;
                }
                currentNode = currentNode.Next;
            }

            Assert.That(nodeCount, Is.EqualTo(numberValidNodes), "Nodes in linked list not accounted for.");
        }

        protected static IEnumerable<Task<bool>> GenerateTasks(int numberOfTasks, int startIndex, Func<int, bool> taskFunction)
        {
            var taskList = new List<Task<bool>>();
            for (var i = startIndex; i < numberOfTasks + startIndex; i++)
            {
                var index = i;
                taskList.Add(new Task<bool>(() => taskFunction(index)));
            }
            return taskList;
        }
    }
}
