using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Domain.Test
{
    [TestClass]
    public class AggregateRootTest
    {
        [TestMethod]
        public void DoImportantStuff()
        {
            var aggregate = new MyAggregate();

            Assert.IsFalse(aggregate.HasDoneImportantStuff);

            aggregate.DoImportantStuff(1337);

            Assert.IsTrue(aggregate.HasDoneImportantStuff);

            var uncommittedEvents = aggregate.UncomittedEvents.ToList();
            Assert.AreEqual(1, uncommittedEvents.Count);
            Assert.AreEqual(0, uncommittedEvents[0].Id);
        }

        [TestMethod]
        public void Load()
        {
            var events = new DomainEvent[] 
            { 
                new ImportantStuffHappend()
                {
                    Id = 0,
                } 
            };

            var aggregate = new MyAggregate();

            Assert.IsFalse(aggregate.HasDoneImportantStuff);

            aggregate.Load(events);

            Assert.IsTrue(aggregate.HasDoneImportantStuff);
            Assert.IsFalse(aggregate.UncomittedEvents.Any());
        }

        [TestMethod]
        public void IdIsIncrementedOnRaisedEvents()
        {
            var aggregate = new MyAggregate();

            Assert.AreEqual(0, aggregate.LessImportantStuffCounter);

            aggregate.DoLessImportantStuff();
            aggregate.DoLessImportantStuff();
            aggregate.DoLessImportantStuff();

            Assert.AreEqual(3, aggregate.LessImportantStuffCounter);

            var uncommittedEvents = aggregate.UncomittedEvents.ToList();
            Assert.AreEqual(3, uncommittedEvents.Count);
            Assert.AreEqual(0, uncommittedEvents[0].Id);
            Assert.AreEqual(1, uncommittedEvents[1].Id);
            Assert.AreEqual(2, uncommittedEvents[2].Id);
        }

        public sealed class ImportantStuffHappend : DomainEvent
        {

        }

        public sealed class LessImportantStuffHappend : DomainEvent
        {

        }

        public class MyAggregate : AggregateRoot
        {
            private bool _hasDoneImportantStuff = false;
            private int _lessImportantStuffCounter = 0;

            public bool HasDoneImportantStuff => _hasDoneImportantStuff;
            public int LessImportantStuffCounter => _lessImportantStuffCounter;

            private void Apply(ImportantStuffHappend e)
            {
                _hasDoneImportantStuff = true;
            }

            private void Apply(LessImportantStuffHappend e)
            {
                _lessImportantStuffCounter++;
            }

            public void DoImportantStuff(int argument)
            {
                Assert.IsFalse(_hasDoneImportantStuff, "DoImportantStuff can only be done once");

                Raise(new ImportantStuffHappend());

                Assert.IsTrue(_hasDoneImportantStuff);
            }

            public void DoLessImportantStuff()
            {
                var count = _lessImportantStuffCounter;

                Raise(new LessImportantStuffHappend());

                Assert.AreEqual(count + 1, _lessImportantStuffCounter);
            }
        }
    }
}
