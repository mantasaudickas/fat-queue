using FatQueue.Messenger.Core;
using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql.Data
{
    internal class MessageContext
    {
        public string QueueName { [UsedImplicitly] get; set; }
        public PublishSettings Settings { get; set; }
        public bool PublishedAsFirstInTheQueue { get; set; }
    }
}
