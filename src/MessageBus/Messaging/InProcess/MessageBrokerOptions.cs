using System;

namespace MessageBus.Messaging.InProcess
{
    public sealed class MessageBrokerOptions
    {
        public MessageBrokerOptions(ISchedulerFactory collectScheduler, ISchedulerFactory executeScheduler)
        {
            CollectScheduler = collectScheduler ?? throw new ArgumentNullException(nameof(collectScheduler));
            ExecuteScheduler = executeScheduler ?? throw new ArgumentNullException(nameof(executeScheduler));
        }

        /// <summary>
        /// Creates an options instance with the default options.
        /// </summary>
        public static MessageBrokerOptions Default()
        {
            return new MessageBrokerOptions(
                new Scheduler.BackgroundThreadSchedulerFactory(nameof(InProcessMessageBroker) + "Thread.Collect"),
                new Scheduler.BackgroundThreadSchedulerFactory(nameof(InProcessMessageBroker) + "Thread.Execute")
            );
        }

        /// <summary>
        /// Creates an options instance which is useful when using manual event execution.
        /// </summary>
        /// <remarks>Do not use this options in production.</remarks>
        public static MessageBrokerOptions BlockingManual(Scheduler.ManualScheduler scheduler)
        {
            return new MessageBrokerOptions(scheduler, scheduler)
            {
                EventExecuter = new BlockingEventExecuter()
            };
        }

        public ISchedulerFactory CollectScheduler { get; }

        public ISchedulerFactory ExecuteScheduler { get; }

        public IEventExecuter EventExecuter { get; init; } = new TaskedEventExecution();
    }
}
