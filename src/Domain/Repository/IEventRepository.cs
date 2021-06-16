using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repository
{
    public interface IEventRepository
    {
        Task<IEnumerable<DomainEvent>> LoadAsync(string id);
        IAsyncEnumerable<DomainEvent> LoadAsyncEnumerable(string id);
        Task SaveAsync(string id, IEnumerable<DomainEvent> domainEvents);
    }

}
