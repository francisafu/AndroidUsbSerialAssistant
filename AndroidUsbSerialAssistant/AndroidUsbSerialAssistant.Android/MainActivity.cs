using Android.App;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Platform = Xamarin.Essentials.Platform;

namespace AndroidUsbSerialAssistant.Droid
{
    [Activity(Label = "@string/app_name",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize
                               | ConfigChanges.Orientation
                               | ConfigChanges.Locale)]

    // The following line is used to received the USB device attached notification
    [IntentFilter(new[]
    {
        UsbManager.ActionUsbDeviceAttached,
        UsbManager.ActionUsbDeviceDetached,
        UsbManager.ActionUsbAccessoryAttached,
        UsbManager.ActionUsbAccessoryDetached
    })]
    // The following line is used to limit the devices that could be found to what we want
    [MetaData(UsbManager.ActionUsbDeviceAttached,
        Resource = "@xml/device_filter")]
    [MetaData(UsbManager.ActionUsbDeviceDetached,
        Resource = "@xml/device_filter")]
    [MetaData(UsbManager.ActionUsbAccessoryAttached,
        Resource = "@xml/device_filter")]
    [MetaData(UsbManager.ActionUsbAccessoryDetached,
        Resource = "@xml/device_filter")]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);
            Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode,
            string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode,
                permissions,
                grantResults);

            base.OnRequestPermissionsResult(requestCode,
                permissions,
                grantResults);
        }
    }
}