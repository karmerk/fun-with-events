using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Infrastructure
{
    public sealed record Event
    {
        public Event(int id, string type, string data)
        {
            // Id or position
            Id = id;
            Type = type;
            Data = data;
        }

        public int Id { get; }
        public string Type { get; }
        public string Data { get; }
    }

    public interface IEventStore
    {
        public Task AppendAsync(string streamName, IEnumerable<Event> events);

        public Task<IEnumerable<Event>> GetAsync(string streamName, int? begin = null, int count = 50);
        
        public Task<IEnumerable<Event>> GetBackwardsAsync(string streamName, int count = 50);

        // TODO Async enumerable types
        //public IAsyncEnumerable<Event> GetAsyncEnumerable(string name, int? begin = null, int? count = null);
        //public IAsyncEnumerable<Event> GetBackwardsAsyncEnumerable(string name, int? count = null);
    }
}
