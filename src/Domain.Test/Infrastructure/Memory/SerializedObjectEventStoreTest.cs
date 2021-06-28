using Domain.Infrastructure;
using Domain.Infrastructure.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Domain.Test.Infrastructure.Memory
{
    [TestClass]
    public class SerializedObjectEventStoreTest : EventStoreTestBase
    {
        public override IEventStore EventStore { get; } = new SerializedObjectEventStore();
    }
}
