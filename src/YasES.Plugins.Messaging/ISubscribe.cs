﻿using System;

namespace YasES.Plugins.Messaging
{
    public interface ISubscribe
    {
        /// <summary>
        /// Add a new subscription to the current channel. The
        /// passed <paramref name="messageHandler"/> will get called
        /// for each event which got published to the current channel.
        /// Note: depending on the used broker scheduler, the
        /// <paramref name="messageHandler"/> callback will get called
        /// from multiple threads and might even get called simultaniously
        /// for different events. The <typeparamref name="T"/> should
        /// match the type of the message which gets send with
        /// <see cref="IPublish.Publish{T}(T)"/>.
        /// </summary>
        IDisposable Subscribe<T>(Action<IMessage<T>> messageHandler);

        /// <summary>
        /// Add a new subscription to the current channel. The
        /// passed <paramref name="messageHandler"/> will get called
        /// for each event which got published to the current channel.
        /// Note: depending on the used broker scheduler, the
        /// <paramref name="messageHandler"/> callback will get called
        /// from multiple threads and might even get called simultaniously
        /// for different events.
        /// </summary>
        IDisposable Subscribe(Action<IMessage> messageHandler);
    }
}
