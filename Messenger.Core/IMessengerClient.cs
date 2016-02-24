using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerClient
    {
        [UsedImplicitly]
        void Publish<T>(Expression<Action<T>> methodCall, QueueName queueName, PublishSettings publishSettings = null);

        [UsedImplicitly]
        void Publish<T>(Expression<Action<T>> methodCall, string queueName = null, PublishSettings publishSettings = null);

        [UsedImplicitly]
        bool Cancel(Guid identity);
    }
}
