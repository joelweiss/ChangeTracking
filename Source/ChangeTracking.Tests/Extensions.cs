using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ChangeTracking.Tests
{
    public static class Extensions
    {
        public static EventMonitor MonitorListChanged(this IBindingList bindingList)
        {
            return new EventMonitor<ListChangedEventHandler>(bindingList, nameof(IBindingList.ListChanged), action => (_, __) => action());
        }

        public static EventMonitor MonitorListChanged<T>(this IList<T> bindingList)
        {
            return new EventMonitor<ListChangedEventHandler>(bindingList, nameof(IBindingList.ListChanged), action => (_, __) => action());
        }

        public static EventMonitor MonitorStatusChanged(this object trackable)
        {
            return new EventMonitor<EventHandler>(trackable, nameof(IChangeTrackable.StatusChanged), action => (_, __) => action());
        }
    }

    internal class EventMonitor<T> : EventMonitor
    {
        internal EventMonitor(object source, string eventName, Func<Action, T> getEventHandler)
        {
            source.GetType().GetEvent(eventName).AddEventHandler(source, getEventHandler(() => WasRaised = true) as Delegate);
        }
    }

    public abstract class EventMonitor
    {
        public bool WasRaised { get; protected set; }
    }
}
