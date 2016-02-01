namespace FatQueue.Messenger.Tests.Handlers
{
    public interface IHandler<TRequest>
    {
        void Handle(TRequest request);
    }
}
