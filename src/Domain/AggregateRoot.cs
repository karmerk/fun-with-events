using System;
using System.Linq;
using System.Collections.Generic;

namespace Domain
{
    public abstract class AggregateRoot
    {
        private List<IDomainEvent> _events = new List<IDomainEvent>();
        private List<IDomainEvent> _uncommitted = new List<IDomainEvent>();

        public IEnumerable<IDomainEvent> UncomittedEvents => _uncommitted;

        internal void ClearUncomittedEvents() => _uncommitted.Clear();

        public AggregateRoot()
        {

        }

        public void Load(IEnumerable<IDomainEvent> domainEvents)
        {
            foreach(var domainEvent in domainEvents)
            {
                Raise(domainEvent);
                ClearUncomittedEvents();
            }
        }

        public void Raise<T>(T domainEvent) where T : IDomainEvent
        {
            // TODO build a map from all apply methods using compiled expressions so we can execute raise faster
            // Also should prioritize match on exact type
            var methods = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(x => x.Name == "Apply").ToArray();
            var type = domainEvent.GetType();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var parameter = parameters[0];

                if(parameter.ParameterType.IsAssignableFrom(type))
                {
                    method.Invoke(this, new object[] { domainEvent });
                    return;
                }
            }

            // throw new no-matching-apply-method-found-exception
        }
    }


}
