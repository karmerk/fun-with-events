using System;
using System.Linq;

namespace Domain
{
    public interface ISupportSnapshot {
        internal ISnapshot GetSnapshot();

        internal void RestoreSnapshot(ISnapshot snapshot);
    }


    public abstract class AggregateRoot<TState> : AggregateRoot, ISupportSnapshot where TState : class, new()
    {
        protected TState State { get; set; } = new();

        private ISnapshot _snapshot = new Snapshot<TState>(new(), -1);


        protected override int GetNextDomainEventId() => Math.Max(_snapshot.LastEventId, UncomittedEvents.LastOrDefault()?.Id ?? -1) + 1;

        ISnapshot ISupportSnapshot.GetSnapshot()
        {
            _snapshot = new Snapshot<TState>(State, Events.LastOrDefault()?.Id ?? -1);

            return _snapshot;

        }

        void ISupportSnapshot.RestoreSnapshot(ISnapshot snapshot)
        {
            var typed = snapshot as Snapshot<TState>;

            State = typed?.State ?? throw new InvalidOperationException($"Snapshot was not of type {typeof(Snapshot<TState>)}");

            _snapshot = typed;
        }
       
    }


    public interface ISnapshot
    {
        int LastEventId { get; }
    }

    public record Snapshot<T> : ISnapshot
    {
        public T State { get; }
        public int LastEventId { get; } = -1;

        public Snapshot(T state, int lastEventId)
        {
            State = state;
            LastEventId = lastEventId;
        }
    }
}
