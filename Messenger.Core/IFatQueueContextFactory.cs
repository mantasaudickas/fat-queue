using System;

namespace FatQueue.Messenger.Core
{
    public interface IFatQueueContextFactory
    {
        IFatQueueContext Create();
    }

    public interface IFatQueueContext : IDisposable
    {
    }

    public class FatQueueContextFactory : IFatQueueContextFactory
    {
        public IFatQueueContext Create()
        {
            return new FatQueueContext();
        }
    }

    public class FatQueueContext : ExecutionContext, IFatQueueContext
    {
    }
}
