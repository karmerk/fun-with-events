using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Test
{
    public class TestDomainEventGenerator
    {
        public IEnumerable<T> Generated<T>(int id, int count) where T : DomainEvent, new()
        {
            foreach(var i in Enumerable.Range(id, count))
            {
                yield return new T() { Id = i };
            }
        }
    }
}
