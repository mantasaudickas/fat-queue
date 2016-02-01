using System;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Tools;
using FatQueue.Messenger.Tests.Events;

namespace FatQueue.Messenger.Tests.Handlers
{
    public class FatQueuePrintMessageEventHandler : IHandler<FatQueuePrintMessageEvent>
    {
        public void Handle(FatQueuePrintMessageEvent request)
        {
            TimeSpan.FromMilliseconds(100).Sleep(new TraceLogger(true));
            Console.WriteLine(request.Message.Message);
        }
    }
}
