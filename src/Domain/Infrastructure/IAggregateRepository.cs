using Domain.Infrastructure.Serialization;
using Domain.Infrastructure.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IEventStore _eventStore;
        
        // TODO Serializer needs to be replaceable 
        private readonly ISerializer _serializer = new TextJsonSerializer();


        public AggregateRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<T> GetAsync<T>(string name) where T : AggregateRoot, new()
        {
            var position = -1;
            var count = 50;

            var aggregate = new T();

            List<Event> events = null!;

            do
            {
                var get = await _eventStore.GetAsync(name, position, count);

                events = get.ToList();

                aggregate.Load(events.Select(Deserialize));

                position = events.LastOrDefault()?.Position ?? -1;

            } while (events.Count == count);

            return aggregate;
        }

        public async Task SaveAsync<T>(string name, T aggregate) where T : AggregateRoot
        {
            var events = aggregate.UncomittedEvents.Select(Serialize).ToList();

            await _eventStore.AppendAsync(name, events);

            aggregate.ClearUncomittedEvents();
        }


        private Event Serialize(DomainEvent domainEvent)
        {
            // TODO need to be possible to replace Type resolving
            var typeName = domainEvent.GetType().AssemblyQualifiedName ?? throw new ApplicationException("Type does not have a AssemblyQualifiedName ??");
            var bytes = _serializer.Serialize(domainEvent);

            return new Event(domainEvent.Id, typeName, bytes);
        }

        private DomainEvent Deserialize(Event @event)
        {
            // TODO need to be possible to replace Type resolving
            var type = Type.GetType(@event.Type) ?? throw new InvalidOperationException($"Unknown type: {@event.Type}");

            return _serializer.Deserialize(type, @event.Data) as DomainEvent ?? throw new InvalidOperationException("Deserialized object was not a DomainEvent");
        }
    }

}
