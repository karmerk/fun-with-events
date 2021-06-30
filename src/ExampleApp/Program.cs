using Domain;
using Domain.Infrastructure;
using Domain.Infrastructure.Memory;
using System;
using System.Threading.Tasks;

namespace ExampleApp
{
    class Program
    {
        private static void ResetLine()
        {
            var (_, top) = Console.GetCursorPosition();

            Console.SetCursorPosition(0, top);
        }

        static async Task Main(string[] args)
        {
            IAggregateRepository repository = new AggregateRepository(new MemoryBasedEventStore());

            var name = "initial";
            var counter = await repository.GetAsync<Counter>(name);

            Console.WriteLine("Unknown command please try again");
            Console.WriteLine("O = Open counter");
            Console.WriteLine("S = Save counter");
            Console.WriteLine("+ = Increment counter");
            Console.WriteLine("- = Decrement counter");
            Console.WriteLine("Esc = Exit");

            var exit = false;

            while (!exit)
            {
                var cmd = Console.ReadKey();

                ResetLine();

                try
                {
                    switch (cmd.Key)
                    {
                        case ConsoleKey.O:
                            Console.Write("Enter name of counter to open: ");
                            name = Console.ReadLine() ?? throw new ArgumentNullException("No name??");
                            counter = await repository.GetAsync<Counter>(name);
                            Console.WriteLine($"Opened counter: Name={name}, Count={counter.Value}");
                            break;
                        case ConsoleKey.S:
                            await repository.SaveAsync(name, counter);
                            Console.WriteLine($"Saved counter: Name={name}, Count={counter.Value}");
                            break;
                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                            counter.Increment();
                            Console.WriteLine($"Increment: Count={counter.Value}");
                            break;
                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                            counter.Decrement();
                            Console.WriteLine($"Decrement: Count={counter.Value}");
                            break;
                        case ConsoleKey.X:
                        case ConsoleKey.Escape:
                            Console.WriteLine("Exit");
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Unknown command please try again");
                            Console.WriteLine("O = Open counter");
                            Console.WriteLine("S = Save counter");
                            Console.WriteLine("+ = Increment counter");
                            Console.WriteLine("- = Decrement counter");
                            Console.WriteLine("Escape or X = Exit");
                            break;
                    }

                }
                catch(Exception exception)
                {
                    Console.WriteLine($"Ooopsie - something went wrong: {exception.Message}");
                }
            }
        }
    }

    public class Counter : AggregateRoot
    {
        private int _count;

        public int Value => _count;

        public class CountIncremented : DomainEvent
        {

        }

        public class CountDecremented : DomainEvent
        {

        }

        private void Apply(CountIncremented e)
        {
            _count++;
        }

        private void Apply(CountDecremented e)
        {
            _count--;
        }

        public void Increment()
        {
            Raise(new CountIncremented());
        }

        public void Decrement()
        {
            if(_count == 0)
            {
                throw new InvalidOperationException("Count is already zero cant decrement further");
            }

            Raise(new CountDecremented());
        }
    }

}
