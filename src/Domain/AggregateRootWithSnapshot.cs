using System;

namespace Domain
{
    public abstract class AggregateRootWithSnapshot<TSnapshot> : AggregateRoot
    {
        public abstract TSnapshot Snapshot { get; internal set; }
    }

}
