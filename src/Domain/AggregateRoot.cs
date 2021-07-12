using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Domain
{
    public abstract class AggregateRoot
    {
        private readonly List<DomainEvent> _events = new List<DomainEvent>();
        private readonly List<DomainEvent> _uncommitted = new List<DomainEvent>();

        internal IEnumerable<DomainEvent> Events => _events.ToImmutableArray();
        internal IEnumerable<DomainEvent> UncomittedEvents => _uncommitted.ToImmutableArray();

        internal void ClearUncomittedEvents() => _uncommitted.Clear();
        
        public AggregateRoot()
        {

        }

        protected virtual int GetNextDomainEventId() => (_events.LastOrDefault()?.Id ?? -1) + 1;


        public void Load(IEnumerable<DomainEvent> domainEvents)
        {
            if(_uncommitted.Count > 0)
            {
                throw new InvalidOperationException($"{nameof(Load)} can not be called with when the aggregated has uncomitted events");
            }

            var expected = GetNextDomainEventId();

            foreach (var domainEvent in domainEvents)
            {
                if(domainEvent.Id != expected)
                {
                    throw new InvalidOperationException($"{nameof(DomainEvent)}s must be loaded ordered by {nameof(domainEvent.Id)}");
                }

                expected++;
            }
                        
            foreach (var domainEvent in domainEvents)
            {
                Raise(domainEvent, false);
            }
        }

        private void Raise<T>(T domainEvent, bool @new) where T : DomainEvent
        {
            // TODO build a map from all apply methods using compiled expressions so we can execute raise faster
            // Also should prioritize match on exact type
            var methods = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(x => x.Name == "Apply").ToArray();
            var type = domainEvent.GetType();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var parameter = parameters[0];

                if (parameter.ParameterType.IsAssignableFrom(type))
                {
                    if (@new)
                    {
                        domainEvent.Id = GetNextDomainEventId();
                    }

                    method.Invoke(this, new object[] { domainEvent });

                    if (@new)
                    {
                        _uncommitted.Add(domainEvent);
                    }
                    _events.Add(domainEvent);
                    return;
                }
            }

            // throw new no-matching-apply-method-found-exception
        }

        protected void Raise<T>(T domainEvent) where T : DomainEvent
        {
            Raise(domainEvent, true);
        }
    }


}
