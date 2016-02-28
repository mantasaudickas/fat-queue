using System;
using System.Linq.Expressions;
namespace FatQueue.Messenger.Core
{
    public interface IMessengerClient
    {
        void Publish<T>(Expression<Action<T>> methodCall, QueueName queueName, PublishSettings publishSettings = null);

        void Publish<T>(Expression<Action<T>> methodCall, string queueName = null, PublishSettings publishSettings = null);

        bool Cancel(Guid identity);
    }
}
