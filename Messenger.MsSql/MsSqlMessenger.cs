using FatQueue.Messenger.Core.Database;

namespace FatQueue.Messenger.MsSql
{
    public class MsSqlMessenger : Core.Services.Messenger
    {
        public MsSqlMessenger(SqlSettings settings) : base(settings, new MsSqlRepositoryFactory(settings))
        {
        }
    }
}
