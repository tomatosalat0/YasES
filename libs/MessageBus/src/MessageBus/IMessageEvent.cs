using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageEvent : IBusMessage
    {
    }

    public interface IMessageEventHandler<in TEvent> where TEvent : IMessageEvent
    {
        void Handle(TEvent @event);
    }

    public interface IAsyncMessageEventHandler<in TEvent> where TEvent : IMessageEvent
    {
        Task HandleAsync(TEvent @event);
    }
}
