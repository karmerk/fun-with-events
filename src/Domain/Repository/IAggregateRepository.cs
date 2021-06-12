using System.Threading.Tasks;

namespace Domain.Repository
{
    public interface IAggregateRepository
    {
        Task<T> GetAsync<T>(string id) where T : AggregateRoot, new();

        Task SaveAsync<T>(T aggregate, string id) where T : AggregateRoot;
    }

    public class AggregateRepository : IAggregateRepository
    {
        private readonly IEventRepository _eventRepository;

        public AggregateRepository(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<T> GetAsync<T>(string id) where T : AggregateRoot, new()
        {
            var aggregate = new T();
            var events = await _eventRepository.LoadAsync(id);

            aggregate.Load(events);

            return aggregate;
        }

        public async Task SaveAsync<T>(T aggregate, string id) where T : AggregateRoot
        {
            await _eventRepository.SaveAsync(id, aggregate.UncomittedEvents);

            aggregate.ClearUncomittedEvents();
        }
    }

}
