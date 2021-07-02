using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Domain.Test.AggregateRootTests
{
    [TestClass]
    public class RaiseTest
    {
        [TestMethod]
        public void Raise_ProducesUncomittedEvents()
        {
            var aggregate = new CountingTestAggregate();
            Assert.AreEqual(0, aggregate.UncomittedEvents.Count());

            aggregate.Increment();
            Assert.AreEqual(1, aggregate.UncomittedEvents.Count());
        }

        [TestMethod]
        public void Raise_IdIsIncremented()
        {
            var aggregate = new CountingTestAggregate();

            Assert.AreEqual(0, aggregate.CurrentCount);

            aggregate.Increment();
            aggregate.Increment();
            aggregate.Decrement();

            var uncommittedEvents = aggregate.UncomittedEvents.ToList();
            Assert.AreEqual(3, uncommittedEvents.Count);
            Assert.AreEqual(0, uncommittedEvents[0].Id);
            Assert.AreEqual(1, uncommittedEvents[1].Id);
            Assert.AreEqual(2, uncommittedEvents[2].Id);
        }
    }
}
