using Domain.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Infrastructure.Memory
{
    public class MemoryBasedEventStore : IEventStore
    {
        private readonly Dictionary<string, Dictionary<int, Event>> _streams = new Dictionary<string, Dictionary<int, Event>>();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task AppendAsync(string streamName, IEnumerable<Event> events)
        {
            var stream = _streams.GetOrAdd(streamName, () => new Dictionary<int, Event>());
            var list = events.ToList();

            if( list.Select(x=>x.Position).Distinct().Count() != list.Count)
            {
                throw new InvalidOperationException($"Concurrency problem detected. One or more positions are used multiple times");
            }

            foreach (var item in list)
            {
                if (stream.ContainsKey(item.Position))
                {
                    throw new InvalidOperationException($"Concurrency problem detected. Position already exists: StreamName={streamName}, Position={item.Position}");
                }
            }

            foreach(var item in list)
            {
                stream.Add(item.Position, item);
            }
        }

        public async Task<IEnumerable<Event>> GetAsync(string streamName, int? begin = null, int count = 50)
        {
            var stream = _streams.GetValueOrDefault(streamName) ?? new Dictionary<int, Event>();
            var queryable = stream.Values.AsQueryable();

            if (begin != null)
            {
                queryable = queryable.Where(x => x.Position >= begin);
            }

            queryable = queryable.OrderBy(x => x.Position);
            queryable = queryable.Take(count);

            return queryable.ToList();
        }

        public async Task<IEnumerable<Event>> GetBackwardsAsync(string streamName, int count = 50)
        {
            var stream = _streams.GetValueOrDefault(streamName) ?? new Dictionary<int, Event>();
            var queryable = stream.Values.AsQueryable();

            queryable = queryable.OrderByDescending(x => x.Position);
            queryable = queryable.Take(count);

            return queryable.ToList();
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
