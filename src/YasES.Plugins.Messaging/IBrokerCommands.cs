using System.Threading;
using System.Threading.Tasks;

namespace YasES.Plugins.Messaging
{
    public interface IBrokerCommands
    {
        /// <summary>
        /// Waits for the <paramref name="millisecondsTimeout"/> or until a new message might
        /// be ready for sending. Returns true if a message might be ready, otherwise false.
        /// </summary>
        /// <exception cref="TaskCanceledException">Is thrown if <paramref name="cancellationToken"/> is canceled.</exception>
        bool WaitForMessages(int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Perform a cleanup of the number of channels.
        /// </summary>
        void RemoveEmptyChannels();

        /// <summary>
        /// Iterates over all queues and sends the first
        /// pending message of each queue. Returns the
        /// number of delivered messages. To drain
        /// the broker, you have to call this method
        /// in a loop until it returns 0.
        /// </summary>
        int CallSubscribers();
    }
}
