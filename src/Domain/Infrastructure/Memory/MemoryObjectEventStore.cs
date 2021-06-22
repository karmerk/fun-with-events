using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Domain.Infrastructure.Memory
{
    #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public sealed class MemoryObjectEventStore : IEventStore
    {
        private readonly Dictionary<string, Dictionary<int, DomainEvent>> _data = new Dictionary<string, Dictionary<int, DomainEvent>>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public async Task AppendAsync(string name, IEnumerable<DomainEvent> events)
        {
            using(await LockAsync())
            {
                if (!_data.TryGetValue(name, out var values))
                {
                    values = _data[name] = new Dictionary<int, DomainEvent>();
                }

                var list = events.ToList();
                
                // TODO instead check and ensure that the list is ordered
                if(list.Select(x=>x.Id).Distinct().Count() != list.Count)
                {
                    throw new InvalidOperationException($"Concurrency problem detected. None distinct ids found in list of events");
                }

                foreach(var @event in list)
                {
                    if(values.ContainsKey(@event.Id))
                    {
                        throw new InvalidOperationException($"Concurrency problem detected. Id already exists, Name={name}, Id={@event.Id}");
                    }
                }

                foreach (var @event in list)
                {
                    values.Add(@event.Id, @event);
                }
            }
        }

        public async Task<IEnumerable<DomainEvent>> GetAsync(string name, int? begin, int? count)
        {
            using (await LockAsync())
            {
                if (_data.TryGetValue(name, out var events))
                {
                    var queryable = events.Values.AsQueryable();

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
            }
            return Enumerable.Empty<DomainEvent>();
        }

        public async Task<IEnumerable<DomainEvent>> GetBackwardsAsync(string name, int? count)
        {
            using (await LockAsync())
            {
                if (_data.TryGetValue(name, out var events))
                {
                    var queryable = events.Values.AsQueryable();

                    queryable = queryable.OrderByDescending(x => x.Id);

                    if (count != null)
                    {
                        queryable = queryable.Take(count.Value);
                    }

                    return queryable.ToList();
                }
            }
            return Enumerable.Empty<DomainEvent>();
        }
        
        private async Task<IDisposable> LockAsync()
        {
            await _lock.WaitAsync();
            return new Release(_lock);
        }
        
        private class Release : IDisposable
        {
            private readonly SemaphoreSlim _lock;

            public Release(SemaphoreSlim @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }
    #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
