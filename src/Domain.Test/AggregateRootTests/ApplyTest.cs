using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Test.AggregateRootTests
{
    [TestClass]
    public class ApplyTest
    {
        private interface IA { }
        private interface IB { }

        private sealed class A : DomainEvent, IA { }
        private sealed class B : DomainEvent, IB { }


        private sealed class RaiseTestAggregate : Domain.AggregateRoot
        {
            public bool ApplyClassACalled { get; set; }
            public bool ApplyInterfaceACalled { get; set; }

            public bool ApplyClassBCalled { get; set; }
            public bool ApplyInterfaceBCalled { get; set; }

            private void Apply(A _)
            {
                ApplyClassACalled = true;
            }

            private void Apply(IA _)
            {
                ApplyInterfaceACalled = true;
            }

            private void Apply(IB _)
            {
                ApplyInterfaceBCalled = true;
            }

            private void Apply(B _)
            {
                ApplyClassBCalled = true;
            }

            public void ExecuteA() => Raise(new A());
            public void ExecuteB() => Raise(new B());
        }

        [TestMethod]
        public void Raise_ApplyIsCalledBasedOnOrderOfMethods()
        {
            var aggregate = new RaiseTestAggregate();

            aggregate.ExecuteA();

            Assert.IsTrue(aggregate.ApplyClassACalled);
            Assert.IsFalse(aggregate.ApplyInterfaceACalled);

            aggregate.ExecuteB();

            Assert.IsFalse(aggregate.ApplyClassBCalled);
            Assert.IsTrue(aggregate.ApplyInterfaceBCalled);
        }
    }
}
