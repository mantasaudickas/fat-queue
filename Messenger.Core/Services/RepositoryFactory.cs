using System;

namespace FatQueue.Messenger.Core.Services
{
    public class RepositoryFactory
    {
        private readonly Func<IRepository> _factoryFunc;

        public RepositoryFactory(Func<IRepository> factoryFunc)
        {
            _factoryFunc = factoryFunc;
        }

        public IRepository Create()
        {
            return _factoryFunc();
        }
    }
}
