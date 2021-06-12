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
        public class A : IDomainEvent { }

        public class B : IDomainEvent { }
        
        public class C : IDomainEvent { }
        

        [TestMethod]
        public async Task SaveAsync()
        {
            var id = $"{nameof(InMemoryEventRepositoryTest)}+{Guid.NewGuid()}";
            var repository = new InMemoryEventRepository();
            var saveEvents = new IDomainEvent[]
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
