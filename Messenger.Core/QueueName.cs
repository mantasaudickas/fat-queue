namespace FatQueue.Messenger.Core
{
    public class QueueName
    {
        public string Name { get; set; }
        public bool IgnoreQueueSettingsScope { get; set; }

        public QueueName()
        {
        }

        public QueueName(string name)
        {
            Name = name;
        }
    }
}
