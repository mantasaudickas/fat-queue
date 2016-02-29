using System;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace FatQueue.Messenger.Core
{
    [Serializable]
    public class QueueSettingsScope : IDisposable
    {
        private QueueSettingsScope _parentSettingsScope;

        public string QueueName { get; }

        public static QueueSettingsScope Current => Storage.Get();

        public QueueSettingsScope(string queueName, bool overrideParent = false)
        {
            _parentSettingsScope = Storage.Get();

            if (_parentSettingsScope != null && !overrideParent)
            {
                queueName = _parentSettingsScope.QueueName;
            }

            QueueName = queueName;

            Storage.Set(this);
        }

        public void Dispose()
        {
            var previousScope = _parentSettingsScope;
            _parentSettingsScope = null;

            Storage.Set(previousScope);

            GC.SuppressFinalize(this); 
        }

        private static class Storage
        {
            private static readonly string QueueNameScopeKey = "FatQueue.Messenger.QueueSettingsScope." + Guid.NewGuid().ToString("N");

            public static QueueSettingsScope Get()
            {
                QueueSettingsScope scope;
                if (HttpContext.Current != null)
                {
                    scope = HttpContext.Current.Items[QueueNameScopeKey] as QueueSettingsScope;
                }
                else
                {
                    scope = CallContext.LogicalGetData(QueueNameScopeKey) as QueueSettingsScope;
                }

                return scope;
            }

            public static void Set(QueueSettingsScope scope)
            {
                if (HttpContext.Current != null)
                {
                    if (scope == null)
                    {
                        HttpContext.Current.Items.Remove(QueueNameScopeKey);
                    }
                    else
                    {
                        HttpContext.Current.Items[QueueNameScopeKey] = scope;
                    }
                }
                else
                {
                    if (scope == null)
                    {
                        CallContext.FreeNamedDataSlot(QueueNameScopeKey);
                    }
                    else
                    {
                        CallContext.LogicalSetData(QueueNameScopeKey, scope);
                    }
                }
            }
        }
    }
}
