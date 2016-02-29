using System;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Services;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlRepositoryFactory : RepositoryFactory
    {
        public PostgreSqlRepositoryFactory(SqlSettings settings) : base(() => new PostgreSqlRepository(settings))
        {
        }

        public PostgreSqlRepositoryFactory(Func<IRepository> factoryFunc) : base(factoryFunc)
        {
        }
    }
}
