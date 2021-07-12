using Domain.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Test.Infrastructure
{
    [TestClass]
    public class AggregateRepositoryTest
    {
        private readonly EventStoreMock _eventStoreMock = new EventStoreMock();

        // TODO lets to real mocks... NSubstitute ? or just use our own MemoryBasedEventStore
        private class EventStoreMock : IEventStore
        {
            public Dictionary<string, List<Event>> Events {get;} = new();

            public Task AppendAsync(string streamName, IEnumerable<Event> events)
            {
                if(!Events.TryGetValue(streamName, out var list))
                {
                    list = Events[streamName] = new List<Event>();
                }

                list.AddRange(events);

                return Task.CompletedTask;
            }

            public List<(string streamName, int? begin, int count)> CallsToGetAsync { get; } = new List<(string streamName, int? begin, int count)>();
            public List<(string streamName, int count)> CallsToGetBackwardsAsync { get; } = new List<(string streamName, int count)>();

            public Task<IEnumerable<Event>> GetAsync(string streamName, int? begin = null, int count = 50)
            {
                CallsToGetAsync.Add((streamName, begin, count));

                if (Events.TryGetValue(streamName, out var list))
                {
                    list = list.OrderBy(x => x.Id)
                        .Where(x => x.Id >= (begin ?? -1))
                        .Take(count).ToList();

                    return Task.FromResult(list.AsEnumerable());
                }

                return Task.FromResult(Enumerable.Empty<Event>());
            }

            public Task<IEnumerable<Event>> GetBackwardsAsync(string streamName, int count = 50)
            {
                CallsToGetBackwardsAsync.Add((streamName,  count));

                if (Events.TryGetValue(streamName, out var list))
                {
                    list = list.OrderByDescending(x => x.Id)
                        .Take(count).ToList();

                    return Task.FromResult(list.AsEnumerable());
                }

                return Task.FromResult(Enumerable.Empty<Event>());
            }
        }


        private sealed class MyAggregate : AggregateRoot
        {
            public int NumberOfEventsRaised => RaisedEvents.Count;
            public List<DomainEvent> RaisedEvents { get; } = new List<DomainEvent>();

            public sealed class A : DomainEvent { }

            public sealed class B : DomainEvent { }

            private void Apply(DomainEvent domainEvent)
            {
                RaisedEvents.Add(domainEvent);
            }

            public void ExecuteFunctionA() => Raise(new A());

            public void ExecuteFunctionB() => Raise(new B());
        }

        [TestMethod]
        public async Task SaveAsync_UncomittedEventsAreSaved()
        {
            var streamName = nameof(SaveAsync_UncomittedEventsAreSaved);
            var aggregate = new MyAggregate();

            aggregate.ExecuteFunctionA();
            aggregate.ExecuteFunctionB();

            var repository = new AggregateRepository(_eventStoreMock);

            await repository.SaveAsync(streamName, aggregate);
            
            Assert.AreEqual(2, _eventStoreMock.Events[streamName].Count);

            var a = _eventStoreMock.Events[streamName][0];
            var b = _eventStoreMock.Events[streamName][1];

            // Test dependent on Type name resovling strategy, does not seem like a good approach
            Assert.AreEqual(typeof(MyAggregate.A).AssemblyQualifiedName, a.Type);
            Assert.AreEqual(typeof(MyAggregate.B).AssemblyQualifiedName, b.Type);
        }

        public async Task SaveAsync_UncomittedEventsAreCleared()
        {
            var streamName = nameof(SaveAsync_UncomittedEventsAreSaved);
            var repository = new AggregateRepository(_eventStoreMock);
            var aggregate = new MyAggregate();

            aggregate.ExecuteFunctionA();
            aggregate.ExecuteFunctionB();

            
            Assert.AreEqual(2, aggregate.UncomittedEvents.Count());

            await repository.SaveAsync(streamName, aggregate);
            Assert.AreEqual(0, aggregate.UncomittedEvents.Count());
        }

        [TestMethod]
        public async Task GetAsync_Returns_EmptyAggregate()
        {
            Assert.AreEqual(0, _eventStoreMock.Events.Count);

            var streamName = Guid.NewGuid().ToString();
            var repository = new AggregateRepository(_eventStoreMock);
            var aggregate = await repository.GetAsync<MyAggregate>(streamName);
                       
            Assert.AreEqual(0, aggregate.NumberOfEventsRaised);
        }

        [TestMethod]
        public async Task GetAsync_GetsAllEvents()
        {
            var streamName = Guid.NewGuid().ToString();
            var events = Enumerable.Range(0, 130).Select(x => new Event(x, typeof(MyAggregate.A).AssemblyQualifiedName, "{}")).ToList();
            _eventStoreMock.Events.Add(streamName, events);
                
            var repository = new AggregateRepository(_eventStoreMock);
            var aggregate = await repository.GetAsync<MyAggregate>(streamName);

            Assert.AreEqual(130, aggregate.NumberOfEventsRaised);
            Assert.AreEqual(0, aggregate.RaisedEvents.First().Id);
            Assert.AreEqual(129, aggregate.RaisedEvents.Last().Id);
        }


        [TestMethod]
        public async Task SaveAsync_AggregateWithState()
        {
            var streamName = Guid.NewGuid().ToString();

            var repository = new AggregateRepository(_eventStoreMock);

            var aggregate = new CountingTestAggregateWithState();

            aggregate.Increment();
            aggregate.Increment();

            await repository.SaveAsync(streamName, aggregate);

            Assert.AreEqual(_eventStoreMock.Events[streamName].Count, 2);
            Assert.AreEqual(_eventStoreMock.Events[$"{streamName}_Snapshot"].Count, 1);
        }


        [TestMethod]
        public async Task GetAsync_AggregateWithState()
        {
            var streamName = Guid.NewGuid().ToString();

            var repository = new AggregateRepository(_eventStoreMock);
            var aggregate = new CountingTestAggregateWithState();

            // Prepare
            aggregate.Increment();
            aggregate.Increment();
            aggregate.Decrement();
            aggregate.Increment();
            Assert.AreEqual(2, aggregate.GetState().Value);

            await repository.SaveAsync(streamName, aggregate);

            Assert.AreEqual(_eventStoreMock.Events[streamName].Count, 4);
            Assert.AreEqual(_eventStoreMock.Events[$"{streamName}_Snapshot"].Count, 1);

            aggregate = await repository.GetAsync<CountingTestAggregateWithState>(streamName);

            Assert.AreEqual(2, aggregate.GetState().Value);
            Assert.AreEqual(0, aggregate.Events.Count());
        }

        [TestMethod]
        public async Task GetAsync_AndAppendAdditionalEvents()
        {
            var streamName = Guid.NewGuid().ToString();

            var repository = new AggregateRepository(_eventStoreMock);
            var aggregate = new CountingTestAggregateWithState();

            // Prepare
            aggregate.Increment();
            aggregate.Increment();
            aggregate.Decrement();
            aggregate.Increment();

            Assert.AreEqual(2, aggregate.GetState().Value);

            await repository.SaveAsync(streamName, aggregate);

            Assert.AreEqual(_eventStoreMock.Events[streamName].Count, 4);
            Assert.AreEqual(_eventStoreMock.Events[$"{streamName}_Snapshot"].Count, 1);

            aggregate = await repository.GetAsync<CountingTestAggregateWithState>(streamName);

            Assert.AreEqual(2, aggregate.GetState().Value);

            aggregate.Increment();
            aggregate.Increment();

            await repository.SaveAsync(streamName, aggregate);

            Assert.AreEqual(_eventStoreMock.Events[streamName].Count, 6);
            Assert.AreEqual(_eventStoreMock.Events[$"{streamName}_Snapshot"].Count, 2); // For now the snapshots corresponds to every time save is called
        }


    }
}

