using System;
using FatQueue.Messenger.Core;
using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql.Data
{
    internal class MessageContext
    {
        public PublishSettings Settings { get; set; }
        public string QueueName { [UsedImplicitly] get; set; }
        public DateTime CreateDate { get; set; }
    }
}
