using System;
using System.Threading;

namespace AndroidUsbSerialDriver.Extensions
{
    internal static class EventHandlerExtensions
    {
        public static void Raise(this EventHandler handler,
            object sender,
            EventArgs e)
        {
            Volatile.Read(ref handler)?.Invoke(sender, e);
        }

        public static void Raise<T>(this EventHandler<T> handler,
            object sender,
            T e) where T : EventArgs
        {
            Volatile.Read(ref handler)?.Invoke(sender, e);
        }
    }
}