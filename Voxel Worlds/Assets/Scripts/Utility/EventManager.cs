using System.Collections.Generic;
using UnityEngine.Events;

namespace Voxel.Utility
{
    public static class EventManager
    {
        private static Dictionary<string, UnityEvent> eventDatabase = new Dictionary<string, UnityEvent>();

        public static void Listen(string eventName, UnityAction eventAction)
        {
            (bool hasEvent, UnityEvent unityEvent) = ContainsEvent(eventName);
            if (hasEvent)
            {
                unityEvent.AddListener(eventAction);
            }
            else
            {
                unityEvent = new UnityEvent();
                unityEvent.AddListener(eventAction);
                eventDatabase.Add(eventName, unityEvent);
            }
        }

        public static void StopListen(string eventName, UnityAction eventAction)
        {
            (bool hasEvent, UnityEvent unityEvent) = ContainsEvent(eventName);
            if (hasEvent)
            {
                unityEvent.RemoveListener(eventAction);
            }
        }

        public static void TriggerEvent(string eventName)
        {
            (bool hasEvent, UnityEvent unityEvent) = ContainsEvent(eventName);
            if (hasEvent)
            {
                unityEvent.Invoke();
            }
        }

        private static (bool, UnityEvent) ContainsEvent(string eventName)
        {
            if (eventDatabase.ContainsKey(eventName))
            {
                return (true, eventDatabase[eventName]);
            }

            return (false, null);
        }
    }
}