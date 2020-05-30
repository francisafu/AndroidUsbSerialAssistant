using Android.App;
using Android.Widget;

namespace AndroidUsbSerialAssistant.Services
{
    public static class ToastService
    {
        public static void ToastShortMessage(string message)
        {
            Toast.MakeText(Application.Context, message, ToastLength.Short)
                .Show();
        }

        public static void ToastLongMessage(string message)
        {
            Toast.MakeText(Application.Context, message, ToastLength.Long)
                .Show();
        }
    }
}