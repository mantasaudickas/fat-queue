using System;
using System.Linq.Expressions;
namespace FatQueue.Messenger.Core
{
    public interface IMessengerClient
    {
        void Publish<T>(
            Expression<Action<T>> action, 
            QueueName queueName = null, 
            PublishSettings publishSettings = null);

        void Publish<T, TContextFactory, TContext>(
            Expression<Action<T>> action, 
            Expression<Func<TContextFactory, TContext>> contextFactory,
            QueueName queueName = null, 
            PublishSettings publishSettings = null)
            where TContext : ExecutionContext;

        bool Cancel(Guid identity);
    }
}
