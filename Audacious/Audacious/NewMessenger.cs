using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacious
{
    public class NewMessenger
    {
        private static NewMessenger instance;
        static List<KeyValuePair<object, object>> eventPairs = new List<KeyValuePair<object, object>>();
        static readonly object lockVar = new object();

        private NewMessenger()
        {
        }

        public static NewMessenger Default
        {
            get 
            {
                if (instance == null)
                    instance = new NewMessenger();

                return instance;
            }
        }

        public void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            eventPairs.Add(new KeyValuePair<object, object>(typeof(TMessage), action));
        }

        public void Unregister<TMessage>(object recipient, Action<TMessage> action)
        {
            lock (lockVar)
            {
                var count = eventPairs.Count();
                for (var i = count - 1; i >= 0 ; i--)
                {
                    var eventPair = eventPairs[i];
                    if (eventPair.Key == typeof(TMessage) && eventPair.Value is Action<TMessage>)
                    {
                        if ((Action<TMessage>)eventPair.Value == action)
                            eventPairs.Remove(eventPair);
                    }
                }
            }
        }

        public void Send<TMessage>(TMessage message)
        {
            var count = eventPairs.Count();
            for (var i = count - 1; i >= 0; i--)
            {
                var eventPair = eventPairs[i];
                if (eventPair.Key == typeof(TMessage))
                {
                    var action = (Action<TMessage>)eventPair.Value;
                    action(message);
                }
            }
        }
    }
}
