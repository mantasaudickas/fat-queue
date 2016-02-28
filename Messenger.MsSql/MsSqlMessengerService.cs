using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.MsSql
{
    public class MsSqlMessengerService : MessengerService
    {
        public MsSqlMessengerService(SqlSettings settings, ILogger logger) : base(logger, new MsSqlRepositoryFactory(settings))
        {
        }
    }
}
