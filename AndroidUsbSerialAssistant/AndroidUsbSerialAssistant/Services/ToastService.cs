using Android.Widget;
using Xamarin.Forms;
using Application = Android.App.Application;

namespace AndroidUsbSerialAssistant.Services
{
    public static class ToastService
    {
        public static void ToastShortMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Toast.MakeText(Application.Context,
                        message,
                        ToastLength.Short)
                    .Show();
            });
        }

        public static void ToastLongMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Toast.MakeText(Application.Context,
                        message,
                        ToastLength.Long)
                    .Show();
            });
        }
    }
}