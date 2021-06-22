using Domain.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Test.Infrastructure.Memory
{
    public abstract class EventStoreTestBase
    {
        private class A : DomainEvent { }
        private class B : DomainEvent { }
        private class C : DomainEvent { }

        public abstract IEventStore EventStore { get; }

        public IEnumerable<DomainEvent> GenerateTestEvents(int firstId = 0)
        {
            var id = firstId;
            while (true)
            {
                yield return new A(){ Id = id++};
                yield return new A(){ Id = id++};
                yield return new B(){ Id = id++};
                yield return new B(){ Id = id++};
                yield return new C(){ Id = id++};
                yield return new C(){ Id = id++};
            }
        }

        [TestMethod]
        public async Task AppendAsync()
        {
            var name = nameof(AppendAsync);

            var events = new DomainEvent[]
            {
                new A(){ Id = 0},
                new A(){ Id = 1},
                new B(){ Id = 2},
                new C(){ Id = 3},
            };

            await EventStore.AppendAsync(name, events);
        }

        [TestMethod,ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public async Task AppendAsync_DuplicatedIdsCausesException()
        {
            var name = nameof(AppendAsync_DuplicatedIdsCausesException);

            var events = new DomainEvent[]
            {
                new A(){ Id = 0},
                new A(){ Id = 1},
                new B(){ Id = 1},
                new C(){ Id = 2},
            };

            // TODO check on specific exception type ? can we expect to know the type of exception?
            //_ = await Assert.ThrowsExceptionAsync<??>(() => EventStore.AppendAsync(name, events));

            await EventStore.AppendAsync(name, events);
        }

        [TestMethod, ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public async Task AppendAsync_AlreadyExistingIdCausesException()
        {
            var name = nameof(AppendAsync_AlreadyExistingIdCausesException);

            var events = new DomainEvent[]
            {
                new A(){ Id = 0},
                new A(){ Id = 1},
                new B(){ Id = 2},
                new C(){ Id = 3},
            };

            await EventStore.AppendAsync(name, events);

            events = new DomainEvent[]
            {
                new C() { Id = 3},
                new A() { Id = 4}
            };

            // TODO check on specific exception type ?
            //_ = await Assert.ThrowsExceptionAsync<??>(() => EventStore.AppendAsync(name, events));
            await EventStore.AppendAsync(name, events);

        }

        [TestMethod]
        public async Task GetAsync()
        {
            var name = nameof(GetAsync);

            var events = new DomainEvent[]
            {
                new A(){ Id = 0},
                new A(){ Id = 1},
                new B(){ Id = 2},
                new C(){ Id = 3},
            };

            await EventStore.AppendAsync(name, events);

            var get = await EventStore.GetAsync(name);
            var result = get?.ToList();
            
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
                        
            Assert.AreEqual(0, result[0].Id);
            Assert.AreEqual(1, result[1].Id);
            Assert.AreEqual(2, result[2].Id);
            Assert.AreEqual(3, result[3].Id);

            Assert.IsTrue(result[0] is A);
            Assert.IsTrue(result[1] is A);
            Assert.IsTrue(result[2] is B);
            Assert.IsTrue(result[3] is C);
        }


        [TestMethod]
        public async Task CanHoldMultipleEventSets()
        {
            await EventStore.AppendAsync("one", GenerateTestEvents().Take(10));
            await EventStore.AppendAsync("two", GenerateTestEvents().Take(15));
            await EventStore.AppendAsync("three", GenerateTestEvents().Take(20));

            var one = (await EventStore.GetAsync("one")).ToList();
            var two = (await EventStore.GetAsync("two")).ToList();
            var three = (await EventStore.GetAsync("three")).ToList();

            Assert.AreEqual(10, one.Count);
            Assert.AreEqual(15, two.Count);
            Assert.AreEqual(20, three.Count);

            var all = one.Concat(two).Concat(three).ToList();
            CollectionAssert.AllItemsAreUnique(all);
        }

        [TestMethod]
        [DataRow(10, 5, 5, DisplayName = "Only 5 events returned even though 10 events exists")]
        [DataRow(5, 10, 5, DisplayName ="All 5 events are returned when count is 10")]
        public async Task GetAsync_WithCount(int numberOfEvetns, int countToRead, int expectedEventsRead)
        {
            var name = nameof(GetAsync_WithCount);

            await EventStore.AppendAsync(name, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(name, count: countToRead)).ToList();

            Assert.AreEqual(expectedEventsRead, result.Count);
            Assert.AreEqual(0, result[0].Id);
        }

        [TestMethod]
        [DataRow(10, 5, 5, DisplayName = "First 5 events are skipped")]
        [DataRow(5, 10, 0, DisplayName = "All events are skipped")]
        public async Task GetAsync_WithBegin(int numberOfEvetns, int begin, int expectedEventsRead)
        {
            var name = nameof(GetAsync_WithBegin);

            await EventStore.AppendAsync(name, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(name, begin: begin)).ToList();

            Assert.AreEqual(expectedEventsRead, result.Count);
        }

        [TestMethod]
        [DataRow(10, 5, 10, 5, DisplayName = "Skip 5 out of 10 events should return 5 events even though count is 10")]
        [DataRow(10, 5, 10, 5, DisplayName = "Skip 5 out of 10 events should return 2 events when count is 2")]
        public async Task GetAsync_WithBeginAndCount(int numberOfEvetns, int begin, int count, int expectedNumberOfEventsRead)
        {
            var name = nameof(GetAsync_WithBeginAndCount);

            await EventStore.AppendAsync(name, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(name, begin: begin)).ToList();

            Assert.AreEqual(expectedNumberOfEventsRead, result.Count);
        }


        [TestMethod]
        public async Task GetGetBackwardsAsync()
        {
            var name = nameof(GetGetBackwardsAsync);

            var events = new DomainEvent[]
            {
                new A(){ Id = 0},
                new A(){ Id = 1},
                new B(){ Id = 2},
                new C(){ Id = 3},
            };

            await EventStore.AppendAsync(name, events);

            var get = await EventStore.GetBackwardsAsync(name);
            var result = get?.ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);

            Assert.AreEqual(0, result[3].Id);
            Assert.AreEqual(1, result[2].Id);
            Assert.AreEqual(2, result[1].Id);
            Assert.AreEqual(3, result[0].Id);

            Assert.IsTrue(result[3] is A);
            Assert.IsTrue(result[2] is A);
            Assert.IsTrue(result[1] is B);
            Assert.IsTrue(result[0] is C);
        }
    }
}
