namespace FatQueue.Messenger.Tests.Events
{
    public class FatQueuePrintMessageEvent
    {
        public IMessage Message { get; set; }

    }

    public interface IMessage
    {
        string Message { get; }
    }

    public class CustomMessage : IMessage
    {
        public string Message { get; set; }
    }
}
