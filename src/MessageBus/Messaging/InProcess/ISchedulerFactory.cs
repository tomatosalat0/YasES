namespace MessageBus.Messaging.InProcess
{
    public interface ISchedulerFactory
    {
        IScheduler Create(IWorkFactory workType);
    }
}
