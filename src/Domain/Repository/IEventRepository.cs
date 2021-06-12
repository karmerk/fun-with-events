using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repository
{
    public interface IEventRepository
    {
        Task<IEnumerable<IDomainEvent>> LoadAsync(string id);
        IAsyncEnumerable<IDomainEvent> LoadAsyncEnumerable(string id);
        Task SaveAsync(string id, IEnumerable<IDomainEvent> domainEvents);
    }

}
