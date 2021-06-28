using Domain.Infrastructure.Serialization;
using Domain.Infrastructure.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Infrastructure.Memory
{
    #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public sealed class SerializedObjectEventStore : IEventStore
    {
        private class DomainEventEntity
        {
            public int Id { get;  }
            public string TypeName { get; }
            public byte[] Bytes { get; }

            public DomainEventEntity(int id, string typeName, byte[] bytes)
            {
                Id = id;
                TypeName = typeName;
                Bytes = bytes;
            }
        }

        // TODO Serializer needs to be replaceable 
        private readonly ISerializer _serializer = new TextJsonSerializer();
        private readonly Dictionary<string, Dictionary<int, DomainEventEntity>> _data = new();

        public async Task AppendAsync(string name, IEnumerable<DomainEvent> events)
        {
            var list = events.ToList();

            // TODO instead check and ensure that the list is ordered
            // TODO make some common sanity checks
            if (list.Select(x => x.Id).Distinct().Count() != list.Count)
            {
                throw new InvalidOperationException($"Concurrency problem detected. None distinct ids found in list of events");
            }

            var data = _data.GetOrAdd(name, () => new Dictionary<int, DomainEventEntity>());

            foreach (var item in list)
            {
                if (data.ContainsKey(item.Id))
                {
                    throw new InvalidOperationException($"Concurrency problem detected. Id already exists, Name={name}, Id={item.Id}");
                }
            }

            var serialized = events.Select(Serialize).ToList();
                        
            foreach (var item in serialized)
            {
                data.Add(item.Id, item);
            }

        }

        private DomainEventEntity Serialize(DomainEvent domainEvent)
        {
            // TODO need to be possible to replace Type resolving
            var typeName = domainEvent.GetType().AssemblyQualifiedName ?? throw new ApplicationException("Type does not have a AssemblyQualifiedName ??");
            var bytes = _serializer.Serialize(domainEvent);

            return new DomainEventEntity(domainEvent.Id, typeName, bytes);
        }

        private DomainEvent Deserialize(DomainEventEntity entity)
        {
            // TODO need to be possible to replace Type resolving
            var type = Type.GetType(entity.TypeName) ?? throw new InvalidOperationException($"Unknown type: {entity.TypeName}");

            return _serializer.Deserialize(type, entity.Bytes) as DomainEvent ?? throw new InvalidOperationException("Deserialized object was not a DomainEvent");
        }

        public async Task<IEnumerable<DomainEvent>> GetAsync(string name, int? begin = null, int? count = null)
        {
            var domainEvents = _data.GetValueOrDefault(name) ?? new Dictionary<int, DomainEventEntity>();
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

            return queryable.Select(Deserialize).ToList();

        }

        public async Task<IEnumerable<DomainEvent>> GetBackwardsAsync(string name, int? count = null)
        {
            var domainEvents = _data.GetValueOrDefault(name) ?? new Dictionary<int, DomainEventEntity>();
            var queryable = domainEvents.Values.AsQueryable();

            queryable = queryable.OrderByDescending(x => x.Id);

            if (count != null)
            {
                queryable = queryable.Take(count.Value);
            }

            return queryable.Select(Deserialize).ToList();
        }
    }
    #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
