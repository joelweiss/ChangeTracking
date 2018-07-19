using System;

namespace ChangeTracking
{
    public class StatusChangedEventArgs : EventArgs
    {
        public object Proxy { get; }
        public ChangeStatus OldStatus { get; }
        public ChangeStatus NewStatus { get; }

        public StatusChangedEventArgs(object proxy, ChangeStatus oldStatus, ChangeStatus newStatus)
        {
            Proxy = proxy;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
