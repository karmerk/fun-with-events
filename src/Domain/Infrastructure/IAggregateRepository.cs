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

            if(aggregate is ISupportSnapshot supportSnapshot)
            {
                var snapshot = (await _eventStore.GetBackwardsAsync($"{name}_Snapshot", 1)).Select(DeserializeSnapshot).FirstOrDefault();
                if (snapshot != null)
                {
                    supportSnapshot.RestoreSnapshot(snapshot);

                    position = snapshot.LastEventId + 1;
                }
            }

            List<Event> events = null!;
            
            do
            {
                var get = await _eventStore.GetAsync(name, position, count);

                events = get.ToList();

                aggregate.Load(events.Select(Deserialize));

                position = (events.LastOrDefault()?.Id ?? -1) + 1;



            } while (events.Count == count);

            return aggregate;
        }

        

        public async Task SaveAsync<T>(string name, T aggregate) where T : AggregateRoot
        {
            var uncomittedEvents = aggregate.UncomittedEvents.Select(Serialize).ToList();
            if (uncomittedEvents.Any())
            {
                if (aggregate is ISupportSnapshot supportSnapshot)
                {
                    // only snapshot every x event
                    var snapshot = supportSnapshot.GetSnapshot();
                    var snapshotEvent = SerializeSnapshot(snapshot);

                    await _eventStore.AppendAsync($"{name}_Snapshot", new[] { snapshotEvent });
                }
                
                await _eventStore.AppendAsync(name, uncomittedEvents);

                aggregate.ClearUncomittedEvents();
            }
        }


        private Event SerializeSnapshot(ISnapshot snapshot)
        {
            var typeName = snapshot.GetType().AssemblyQualifiedName ?? throw new ApplicationException("Type does not have a AssemblyQualifiedName ??");
            var bytes = _serializer.Serialize(snapshot);

            return new Event(snapshot.LastEventId, typeName, bytes);
        }

        private ISnapshot DeserializeSnapshot(Event @event)
        {
            // TODO need to be possible to replace Type resolving
            var type = Type.GetType(@event.Type) ?? throw new InvalidOperationException($"Unknown type: {@event.Type}");
            var snapshot = _serializer.Deserialize(type, @event.Data) as ISnapshot ?? throw new InvalidOperationException("Deserialized object was not a Snapshot");

            return snapshot;
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
            var domainEvent = _serializer.Deserialize(type, @event.Data) as DomainEvent ?? throw new InvalidOperationException("Deserialized object was not a DomainEvent");

            domainEvent.Id = @event.Id;

            return domainEvent;
        }
    }

    internal sealed class SnapshotEvent : DomainEvent
    {
        public int LastEventId { get; set; } = -1;

        public string State { get; set; } = null!;
    }
}
