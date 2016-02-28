using System;

namespace FatQueue.Messenger.Core.Data
{
    public class MessageContext
    {
        public PublishSettings Settings { get; set; }
        public string QueueName { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
