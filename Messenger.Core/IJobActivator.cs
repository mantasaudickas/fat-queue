using System;

namespace FatQueue.Messenger.Core
{
    public interface IJobActivator
    {
        object ActivateJob(Type jobType);
    }
}
