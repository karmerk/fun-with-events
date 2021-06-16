using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Domain
{
      
    public abstract class AggregateRoot
    {
        private List<DomainEvent> _events = new List<DomainEvent>();
        private List<DomainEvent> _uncommitted = new List<DomainEvent>();

        internal IEnumerable<DomainEvent> UncomittedEvents => _uncommitted.ToImmutableArray();

        internal void ClearUncomittedEvents() => _uncommitted.Clear();
        
        public AggregateRoot()
        {

        }

        public void Load(IEnumerable<DomainEvent> domainEvents)
        {
            // TODO - load needs to verify the integrity (specifically the order) of the events

            foreach (var domainEvent in domainEvents)
            {
                Raise(domainEvent, false);
                ClearUncomittedEvents();
            }
        }

        private void Raise<T>(T domainEvent, bool uncomitted) where T : DomainEvent
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
                    if (uncomitted)
                    {
                        domainEvent.Id = (_events.LastOrDefault()?.Id ?? -1)+1;
                    }

                    method.Invoke(this, new object[] { domainEvent });

                    if (uncomitted)
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
