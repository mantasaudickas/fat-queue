using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core.Expressions
{
    [UsedImplicitly]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class JobActivator : IJobActivator
    {
        private readonly Func<Type, object> _factory;

        public JobActivator()
            : this(Activator.CreateInstance)
        {
        }

        public JobActivator(Func<Type, object> factory)
        {
            _factory = factory;
        }

        public virtual object ActivateJob(Type jobType)
        {
            return _factory(jobType);
        }
    }
}