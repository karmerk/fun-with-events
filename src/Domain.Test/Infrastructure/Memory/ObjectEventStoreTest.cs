using Domain.Infrastructure;
using Domain.Infrastructure.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Domain.Test.Infrastructure.Memory
{
    [TestClass]
    public class ObjectEventStoreTest : EventStoreTestBase
    {
        public override IEventStore EventStore { get; } = new ObjectEventStore();
    }
}
