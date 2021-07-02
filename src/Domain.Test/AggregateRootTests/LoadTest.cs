using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Domain.Test.AggregateRootTests
{
    [TestClass]
    public class LoadTest
    {
        private readonly TestDomainEventGenerator _generator = new TestDomainEventGenerator();

        [TestMethod]
        public void Load()
        {
            var aggregate = new CountingTestAggregate();
            var events = _generator.Generated<CountingTestAggregate.IncrementEvent>(0, 42);

            Assert.AreEqual(0, aggregate.CurrentCount);

            aggregate.Load(events);
            Assert.AreEqual(42, aggregate.CurrentCount);
        }

        [TestMethod]
        public void Load_DoesNotLoadEventsAsUncomitted()
        {
            var aggregate = new CountingTestAggregate();
            var events = _generator.Generated<CountingTestAggregate.DecrementEvent>(0, 2);

            Assert.AreEqual(0, aggregate.CurrentCount);

            aggregate.Load(events);
            Assert.AreEqual(-2, aggregate.CurrentCount);
            Assert.AreEqual(0, aggregate.UncomittedEvents.Count());
        }

        

        [TestMethod]
        public void Load_EmptyList()
        {
            var aggregate = new CountingTestAggregate();
            var empty = Enumerable.Empty<DomainEvent>();

            aggregate.Load(empty);
        }

        [TestMethod]
        public void Load_CalledMultipleTimes()
        {
            var aggregate = new CountingTestAggregate();
            var a = _generator.Generated<CountingTestAggregate.IncrementEvent>(0, 3);
            var b = _generator.Generated<CountingTestAggregate.DecrementEvent>(a.Last().Id + 1, 2);
            var c = _generator.Generated<CountingTestAggregate.DecrementEvent>(b.Last().Id + 1, 1);

            aggregate.Load(a);
            aggregate.Load(b);
            aggregate.Load(c);

            Assert.AreEqual(0, aggregate.UncomittedEvents.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void Load_WithUncommittedEventsThrowsException()
        {
            var aggregate = new CountingTestAggregate();
            var events = _generator.Generated<CountingTestAggregate.DecrementEvent>(0, 2);

            Assert.AreEqual(0, aggregate.CurrentCount);

            aggregate.Increment();

            aggregate.Load(events);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void Load_OutOfOrderEventsThrowsException()
        {
            var aggregate = new CountingTestAggregate();
            var a = _generator.Generated<CountingTestAggregate.IncrementEvent>(0, 3);
            var b = _generator.Generated<CountingTestAggregate.DecrementEvent>(a.Last().Id + 1, 2);

            aggregate.Load(b);
            aggregate.Load(a);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void Load_FirstEventMustHaveIdZero()
        {
            const int id = 42;

            var aggregate = new CountingTestAggregate();
            var a = _generator.Generated<CountingTestAggregate.IncrementEvent>(id, 2);

            aggregate.Load(a);
        }
    }
}
