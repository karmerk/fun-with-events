﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Test
{
    public sealed class CountingTestAggregate : AggregateRoot
    {
        public sealed class IncrementEvent : DomainEvent { }

        public sealed class DecrementEvent : DomainEvent { }


        public int CurrentCount { get; private set; }

        private void Apply(IncrementEvent _)
        {
            CurrentCount++;
        }

        private void Apply(DecrementEvent _)
        {
            CurrentCount--;
        }

        public void Increment() => Raise(new IncrementEvent());
        public void Decrement() => Raise(new DecrementEvent());

    }
}
