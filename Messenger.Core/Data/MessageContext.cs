using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core.Data
{
    public class MessageContext
    {
        public PublishSettings Settings { get; set; }
        public string QueueName { [UsedImplicitly] get; set; }
        public DateTime CreateDate { get; set; }
    }
}
