using System.Threading.Tasks;

namespace MessageBus.Messaging
{
    public interface IPublishable
    {
        /// <summary>
        /// Publish the provided <paramref name="message"/> to the
        /// current channel. Publishing a message won't directly
        /// call any subscribers.
        /// </summary>
        Task Publish<T>(T message);
    }
}
