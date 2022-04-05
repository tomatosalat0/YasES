namespace YasES.Plugins.Messaging
{
    public interface IPublish
    {
        /// <summary>
        /// Publish the provided <paramref name="message"/> to the
        /// current channel. Publishing a message won't directly
        /// call any subscribers.
        /// </summary>
        void Publish<T>(T message);
    }
}
