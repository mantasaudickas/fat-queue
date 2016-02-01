using System.Threading;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerServer
    {
        [UsedImplicitly]
        void Start(CancellationToken? cancellationToken);
    }
}
