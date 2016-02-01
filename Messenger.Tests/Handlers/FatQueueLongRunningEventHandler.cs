using System;
using System.Threading;
using FatQueue.Messenger.Tests.Events;

namespace FatQueue.Messenger.Tests.Handlers
{
    public class FatQueueLongRunningEventHandler : IHandler<FatQueueLongRunningEvent>
    {
        public void Handle(FatQueueLongRunningEvent request)
        {
            for (int i = 0; i < 100; ++i)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}
