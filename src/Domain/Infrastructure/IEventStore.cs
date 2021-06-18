using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Infrastructure
{
    public interface IEventStore
    {
        public Task AppendAsync(string name, IEnumerable<DomainEvent> events);

        public Task<IEnumerable<DomainEvent>> GetAsync(string name, int? begin = null, int? count = null);

        public Task<IEnumerable<DomainEvent>> GetBackwards(string name, int? count = null);


        // TODO Async enumerable types
        //public IAsyncEnumerable<DomainEvent> GetAsyncEnumerable(string name, int? begin = null, int? count = null);
        //public IAsyncEnumerable<DomainEvent> GetBackwardsAsyncEnumerable(string name, int? count = null);
    }

}
