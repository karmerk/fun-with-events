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
        }

        [TestMethod]
        public void Load()
        {
            var events = new IDomainEvent[] { new ImportantStuffHappend() { } };
            var aggregate = new MyAggregate();

            Assert.IsFalse(aggregate.HasDoneImportantStuff);

            aggregate.Load(events);

            Assert.IsTrue(aggregate.HasDoneImportantStuff);
            Assert.IsFalse(aggregate.UncomittedEvents.Any());
        }

        public sealed class ImportantStuffHappend : IDomainEvent
        {

        }

        public sealed class LessImportantStuffHappend : IDomainEvent
        {

        }

        public class MyAggregate : AggregateRoot
        {
            private bool _hasDoneImportantStuff = false;
            private int _lessImportantStuffCounter = 0;

            public bool HasDoneImportantStuff => _hasDoneImportantStuff;

            private void Apply(ImportantStuffHappend evnt)
            {
                _hasDoneImportantStuff = true;
            }

            private void Apply(LessImportantStuffHappend evnt)
            {
                _lessImportantStuffCounter++;
            }

            public void DoImportantStuff(int argument)
            {
                if (_hasDoneImportantStuff)
                {
                    throw new InvalidOperationException("DoImportantStuff can only be done once");
                }

                Raise(new ImportantStuffHappend());
            }

            public void DoLessImportantStuff()
            {
                Raise(new LessImportantStuffHappend());
            }
        }
    }
}
