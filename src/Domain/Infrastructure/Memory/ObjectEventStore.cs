using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Domain.Infrastructure.Memory
{

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    
    // Rename to DomainEventStore ?
    public sealed class ObjectEventStore : IEventStore
    {
        private readonly Dictionary<string, Dictionary<int, DomainEvent>> _data = new();

        public async Task AppendAsync(string name, IEnumerable<DomainEvent> events)
        {
            var list = events.ToList();

            // TODO instead check and ensure that the list is ordered
            // TODO make some common sanity checks
            if (list.Select(x => x.Id).Distinct().Count() != list.Count)
            {
                throw new InvalidOperationException($"Concurrency problem detected. None distinct ids found in list of events");
            }

            var values = _data.GetOrAdd(name, () => new Dictionary<int, DomainEvent>());

            foreach(var item in list)
            {
                if(values.ContainsKey(item.Id))
                {
                    throw new InvalidOperationException($"Concurrency problem detected. Id already exists, Name={name}, Id={item.Id}");
                }
            }

            foreach(var item in list)
            {
                values.Add(item.Id, item);
            }
        }

        public async Task<IEnumerable<DomainEvent>> GetAsync(string name, int? begin, int? count)
        {
            var domainEvents = _data.GetValueOrDefault(name) ?? new Dictionary<int, DomainEvent>();
            var queryable = domainEvents.Values.AsQueryable();

            if (begin != null)
            {
                queryable = queryable.Where(x => x.Id >= begin);
            }

            queryable = queryable.OrderBy(x => x.Id);

            if (count != null)
            {
                queryable = queryable.Take(count.Value);
            }

            return queryable.ToList();

        }

        public async Task<IEnumerable<DomainEvent>> GetBackwardsAsync(string name, int? count)
        {
            var domainEvents = _data.GetValueOrDefault(name) ?? new Dictionary<int, DomainEvent>();
            var queryable = domainEvents.Values.AsQueryable();

            queryable = queryable.OrderByDescending(x => x.Id);

            if (count != null)
            {
                queryable = queryable.Take(count.Value);
            }

            return queryable.ToList();

        }
    }
    #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
