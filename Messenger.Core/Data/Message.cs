namespace FatQueue.Messenger.Core.Data
{
    public class Message
    {
        public int QueueId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public string Context { get; set; }
    }
}
