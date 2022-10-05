using System;
using MessageBus.Messaging;

namespace MessageBus
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class TopicAttribute : Attribute
    {
        public TopicAttribute(string topicName)
        {
            Topic = new TopicName(topicName);
        }

        public TopicName Topic { get; }
    }
}
