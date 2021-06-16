using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Domain.Repository.Memory;

namespace Domain.Test.Repository.Memory
{
    [TestClass]
    public class InMemoryEventRepositoryTest
    {
        public class A : DomainEvent { }

        public class B : DomainEvent { }
        
        public class C : DomainEvent { }
        

        [TestMethod]
        public async Task SaveAsync()
        {
            var id = $"{nameof(InMemoryEventRepositoryTest)}+{Guid.NewGuid()}";
            var repository = new InMemoryEventRepository();
            var saveEvents = new DomainEvent[]
            {
                new A(),
                new B(),
                new C()
            };

            await repository.SaveAsync(id, saveEvents);

            var loadEvents = (await repository.LoadAsync(id)).ToArray();

            CollectionAssert.AreEqual(saveEvents, loadEvents);
        }
    }
}
