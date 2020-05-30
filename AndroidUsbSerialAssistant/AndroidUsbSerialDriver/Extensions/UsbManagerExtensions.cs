using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;

namespace AndroidUsbSerialDriver.Extensions
{
    public static class UsbManagerExtensions
    {
        private const string ACTION_USB_PERMISSION =
            "com.Android.UsbSerial.Util.USB_PERMISSION";

        //static readonly Dictionary<Tuple<Context, UsbDevice>, TaskCompletionSource<bool>> taskCompletionSources =
        //    new Dictionary<Tuple<Context, UsbDevice>, TaskCompletionSource<bool>>();

        public static Task<bool> RequestPermissionAsync(this UsbManager manager,
            UsbDevice device,
            Context context)
        {
            var completionSource = new TaskCompletionSource<bool>();

            var usbPermissionReceiver =
                new UsbPermissionReceiver(completionSource);
            context.RegisterReceiver(usbPermissionReceiver,
                new IntentFilter(ACTION_USB_PERMISSION));

            var intent = PendingIntent.GetBroadcast(context,
                0,
                new Intent(ACTION_USB_PERMISSION),
                0);
            manager.RequestPermission(device, intent);

            return completionSource.Task;
        }

        private class UsbPermissionReceiver : BroadcastReceiver
        {
            private readonly TaskCompletionSource<bool> completionSource;

            public UsbPermissionReceiver(
                TaskCompletionSource<bool> completionSource)
            {
                this.completionSource = completionSource;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                var device =
                    intent.GetParcelableExtra(UsbManager.ExtraDevice) as
                        UsbDevice;
                var permissionGranted =
                    intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted,
                        false);
                context.UnregisterReceiver(this);
                completionSource.TrySetResult(permissionGranted);
            }
        }
    }
}