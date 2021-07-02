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
        public abstract IEventStore EventStore { get; }

        public IEnumerable<Event> GenerateTestEvents(int position = 0)
        {
            int i = 0;
            while (true)
            {
                yield return new Event(position++, $"Type_{Guid.NewGuid()}", $"Data_{Guid.NewGuid()}");

                if(i++ > 50000)
                {
                    Assert.Fail("Think you went a little to far.. use .Take(x) to limit the number of test events generated");
                }
            }
        }

        [TestMethod]
        public async Task AppendAsync()
        {
            var streamName = nameof(AppendAsync);

            var events = GenerateTestEvents(0).Take(4);


            await EventStore.AppendAsync(streamName, events);
        }

        [TestMethod,ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public async Task AppendAsync_DuplicatedIdsCausesException()
        {
            var streamName = nameof(AppendAsync_DuplicatedIdsCausesException);

            var events = GenerateTestEvents(0).Take(4).ToList();

            // TODO should be a copy not the same object
            events.Add(events.Last());

            // TODO check on specific exception type ? can we expect to know the type of exception?
            //_ = await Assert.ThrowsExceptionAsync<??>(() => EventStore.AppendAsync(name, events));

            // TODO we need to ensure that the event position 0 is not acutally added

            await EventStore.AppendAsync(streamName, events);
        }

        [TestMethod, ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public async Task AppendAsync_AlreadyExistingIdCausesException()
        {
            var streamName = nameof(AppendAsync_AlreadyExistingIdCausesException);

            var events = GenerateTestEvents().Take(4).ToArray();
            
            await EventStore.AppendAsync(streamName, events);

            events = GenerateTestEvents(events.Last().Id).Take(2).ToArray();

            // TODO check on specific exception type ?
            //_ = await Assert.ThrowsExceptionAsync<??>(() => EventStore.AppendAsync(name, events));
            await EventStore.AppendAsync(streamName, events);

        }

        [TestMethod]
        public async Task GetAsync()
        {
            var streamName = nameof(GetAsync);

            var events = GenerateTestEvents().Take(4).ToArray();
          
            await EventStore.AppendAsync(streamName, events);

            var get = await EventStore.GetAsync(streamName);
            var result = get?.ToList();
            
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
                        
            Assert.AreEqual(0, result[0].Id);
            Assert.AreEqual(1, result[1].Id);
            Assert.AreEqual(2, result[2].Id);
            Assert.AreEqual(3, result[3].Id);

            Assert.AreEqual(events[0].Type, result[0].Type);
            Assert.AreEqual(events[1].Type, result[1].Type);
            Assert.AreEqual(events[2].Type, result[2].Type);
            Assert.AreEqual(events[3].Type, result[3].Type);
        }


        [TestMethod]
        public async Task CanHoldMultipleEventStreams()
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
            var streamName = nameof(GetAsync_WithCount);

            await EventStore.AppendAsync(streamName, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(streamName, count: countToRead)).ToList();

            Assert.AreEqual(expectedEventsRead, result.Count);
            Assert.AreEqual(0, result[0].Id);
        }

        [TestMethod]
        [DataRow(10, 5, 5, DisplayName = "First 5 events are skipped")]
        [DataRow(5, 10, 0, DisplayName = "All events are skipped")]
        public async Task GetAsync_WithBegin(int numberOfEvetns, int begin, int expectedEventsRead)
        {
            var streamName = nameof(GetAsync_WithBegin);

            await EventStore.AppendAsync(streamName, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(streamName, begin: begin)).ToList();

            Assert.AreEqual(expectedEventsRead, result.Count);
        }

        [TestMethod]
        [DataRow(10, 5, 10, 5, DisplayName = "Skip 5 out of 10 events should return 5 events even though count is 10")]
        [DataRow(10, 5, 10, 5, DisplayName = "Skip 5 out of 10 events should return 2 events when count is 2")]
        public async Task GetAsync_WithBeginAndCount(int numberOfEvetns, int begin, int count, int expectedNumberOfEventsRead)
        {
            var streamName = nameof(GetAsync_WithBeginAndCount);

            await EventStore.AppendAsync(streamName, GenerateTestEvents().Take(numberOfEvetns));

            var result = (await EventStore.GetAsync(streamName, begin: begin)).ToList();

            Assert.AreEqual(expectedNumberOfEventsRead, result.Count);
        }


        [TestMethod]
        public async Task GetGetBackwardsAsync()
        {
            var streamName = nameof(GetGetBackwardsAsync);

            var events = GenerateTestEvents().Take(4).ToArray();

            await EventStore.AppendAsync(streamName, events);

            var get = await EventStore.GetBackwardsAsync(streamName);
            var result = get?.ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);

            Assert.AreEqual(events[0].Id, result[3].Id);
            Assert.AreEqual(events[1].Id, result[2].Id);
            Assert.AreEqual(events[2].Id, result[1].Id);
            Assert.AreEqual(events[3].Id, result[0].Id);

            Assert.AreEqual(events[0].Type, result[3].Type);
            Assert.AreEqual(events[1].Type, result[2].Type);
            Assert.AreEqual(events[2].Type, result[1].Type);
            Assert.AreEqual(events[3].Type, result[0].Type);

            Assert.AreEqual(events[0].Data, result[3].Data);
            Assert.AreEqual(events[1].Data, result[2].Data);
            Assert.AreEqual(events[2].Data, result[1].Data);
            Assert.AreEqual(events[3].Data, result[0].Data);
        }
    }
}
