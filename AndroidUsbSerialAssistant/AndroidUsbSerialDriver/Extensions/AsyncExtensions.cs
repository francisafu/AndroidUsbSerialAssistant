using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Hardware.Usb;
using AndroidUsbSerialDriver.Driver.Interface;
using AndroidUsbSerialDriver.Driver.UsbSerialProber;

namespace AndroidUsbSerialDriver.Extensions
{
    public static class AsyncExtensions
    {
        public static Task<IList<IUsbSerialDriver>> FindAllDriversAsync(
            this UsbSerialProber prober,
            UsbManager manager)
        {
            var tcs = new TaskCompletionSource<IList<IUsbSerialDriver>>();

            Task.Run(
                () => { tcs.TrySetResult(prober.FindAllDrivers(manager)); });
            return tcs.Task;
        }
    }
}