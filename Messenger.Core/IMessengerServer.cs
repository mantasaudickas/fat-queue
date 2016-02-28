using System.Threading;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerServer
    {
        void Start(CancellationToken? cancellationToken);
    }
}
