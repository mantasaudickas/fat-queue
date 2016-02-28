namespace FatQueue.Messenger.Core.Data
{
    public class ResultState
    {
        public int ProcessedMessages { get; set; }
        public bool IsFailed { get; set; }
        public string Error { get; set; }
        public int MaxRetries { get; set; }
        public RecoveryMode RecoveryMode { get; set; }
        public int LastMessageId { get; set; }
    }
}
