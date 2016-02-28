namespace FatQueue.Messenger.Core.Data
{
    public class QueueInfo
    {
        public string Name { get; set; }
        public int QueueId { get; set; }
        public int? Retries { get; set; }
    }
}
