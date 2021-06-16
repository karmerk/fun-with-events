using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repository.Memory
{
    public class InMemoryEventRepository : IEventRepository
    {
        private class EventData
        {
            public string Id { get; }

            public EventData(string id)
            {
                Id = id;
            }

            public List<DomainEvent> Events { get; } = new List<DomainEvent>();
        }

        private List<EventData> _datas = new List<EventData>();


        public Task SaveAsync(string id, IEnumerable<DomainEvent> domainEvents)
        {
            var data = _datas.FirstOrDefault(x => x.Id == id);

            if (data == null)
            {
                data = new EventData(id);
                _datas.Add(data);
            }

            data.Events.AddRange(domainEvents);

            return Task.CompletedTask;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<DomainEvent> LoadAsyncEnumerable(string id)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var data = _datas.FirstOrDefault(x => x.Id == id);

            if (data?.Events.Any() ?? false)
            {
                foreach (var evnt in data.Events)
                {
                    yield return evnt;
                }
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IEnumerable<DomainEvent>> LoadAsync(string id)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var data = _datas.FirstOrDefault(x => x.Id == id);

            if (data?.Events.Any() ?? false)
            {
                return data.Events.ToArray();

            }

            return Enumerable.Empty<DomainEvent>();
        }
    }

}
