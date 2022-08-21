using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MessageBus.Examples.MessageBus
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            using Application system = new Application();

            var examples = CollectExamples();
            int index;
            do
            {
                index = MainMenu(examples);
                if (index >= 0)
                {
                    Console.Clear();
                    await RunExample(system, examples[index].Name, examples[index].Execute);
                }
            } while (index >= 0);
        }

        static IReadOnlyList<Example> CollectExamples()
        {
            return new List<Example>()
            {
                new Example("Command example", CommandExample.Execute),
                new Example("Query example", QueryExample.Execute),
                new Example("Dead lock example", DeadLockAvoid.Execute),
                new Example("Query with missing handler example", MissingHandler.Execute),
                new Example("Parallel query example", ParallelQuery.Execute),
                new Example("Inline handler example", InlineHandler.Execute),
                new Example("Distributed Dead Lock", DistributedDeadlock.ExecuteNonWorking),
                new Example("Distributed Dead Lock - Working", DistributedDeadlock.ExecuteWorkingButFlawed)
            };
        }

        static int MainMenu(IReadOnlyList<Example> examples)
        {
            while (true)
            {
                Console.Clear();
                int? value = PrintMain(examples);
                if (value.HasValue)
                    return value.Value;
            }
        }

        static int? PrintMain(IReadOnlyList<Example> examples)
        {
            Console.WriteLine($"Enter the number of the example to execute it (enter nothing to exit)");
            for (int i = 0; i < examples.Count; i++)
            {
                string paddedNumber = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(2, ' ').PadRight(3, ' ');
                string number = ConsoleFormat.Format(paddedNumber, ConsoleFormat.BackgroundColor.White, ConsoleFormat.ForegroundColor.Black);
                Console.WriteLine($"\t{number}: {examples[i].Name}");
            }
            Console.Write("> ");
            string input = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return -1;

            if (!int.TryParse(input, out int result))
                return null;

            if (result < 1 || result > examples.Count)
                return null;

            return result - 1;
        }

        static async Task RunExample(Application system, string description, Func<Application, Task> execute)
        {
            Console.Clear();
            Console.WriteLine(description);
            Console.WriteLine();
            await execute(system);
            Console.WriteLine();
            Console.WriteLine($"{description} complete - press ENTER to continue");
            Console.ReadLine();
        }

        private class Example
        {
            public Example(string name, Func<Application, Task> execute)
            {
                Name = name;
                Execute = execute;
            }

            public string Name { get; }

            public Func<Application, Task> Execute { get; }
        }
    }
}
