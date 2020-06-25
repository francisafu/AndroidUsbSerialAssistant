using Android.Views;
using AndroidUsbSerialAssistant.Controls;
using AndroidUsbSerialAssistant.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Application = Android.App.Application;

[assembly:
    ExportRenderer(typeof(BorderlessEntry), typeof(BorderlessEntryRenderer))]

namespace AndroidUsbSerialAssistant.Droid
{
    public class BorderlessEntryRenderer : EntryRenderer
    {
        public BorderlessEntryRenderer() : base(Application.Context)
        {
        }

        protected override void OnElementChanged(
            ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.SetBackground(null);
                Control.Gravity = GravityFlags.CenterVertical;
                Control.SetPadding(0, 0, 0, 0);
            }
        }
    }
}