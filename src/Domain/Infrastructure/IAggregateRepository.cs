using System.Threading.Tasks;

namespace Domain.Infrastructure
{
    public interface IAggregateRepository
    {
        Task<T> GetAsync<T>(string name) where T : AggregateRoot, new();

        Task SaveAsync<T>(string name, T aggregate) where T : AggregateRoot;
    }

    public class AggregateRepository : IAggregateRepository
    {
        private readonly IEventStore _eventRepository;

        public AggregateRepository(IEventStore eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<T> GetAsync<T>(string name) where T : AggregateRoot, new()
        {
            var aggregate = new T();
            var events = await _eventRepository.GetAsync(name);

            aggregate.Load(events);

            return aggregate;
        }

        public async Task SaveAsync<T>(string name, T aggregate) where T : AggregateRoot
        {
            await _eventRepository.AppendAsync(name, aggregate.UncomittedEvents);

            aggregate.ClearUncomittedEvents();
        }
    }

}
