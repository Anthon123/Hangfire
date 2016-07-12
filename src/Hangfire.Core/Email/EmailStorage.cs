using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Email
{
    public class EmailStorage
    {
        private static readonly object LockObject = new object();
        private static EmailStorage _current;
        private static List<INotifier> _notifiers;
        
        public static EmailStorage Current
        {
            get
            {
                lock (LockObject)
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException("JobStorage.Current property value has not been initialized. You must set it before using Hangfire Client or Server API.");
                    }

                    return _current;
                }
            }
            set
            {
                lock (LockObject)
                {
                    _current = value;
                }
            }
        }

        public EmailStorage(List<INotifier> notifiers)
        {
            _notifiers = notifiers;
        }

        public void NotifyAll(EventTypes.Events eventType, string subject, string message)
        {
            foreach (var notifer in _notifiers)
            {
                notifer.Notify(eventType, subject, message);
            }
        }
    }
}
