using System;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using AndroidUsbSerialAssistant.Resx;
using AndroidUsbSerialDriver.Extensions;
using Xamarin.Forms;
using Application = Android.App.Application;

namespace AndroidUsbSerialAssistant.Services
{
    [BroadcastReceiver(Enabled = true)]
    // The following line is used to received the USB device attached notification
    [IntentFilter(new[]
    {
        UsbManager.ActionUsbDeviceAttached,
        UsbManager.ActionUsbDeviceDetached
    })]
    // The following line is used to limit the devices that could be found to what we want
    [MetaData(UsbManager.ActionUsbDeviceAttached,
        Resource = "@xml/device_filter")]
    [MetaData(UsbManager.ActionUsbDeviceDetached,
        Resource = "@xml/device_filter")]
    public class UsbDeviceStatusBroadcastReceiverService : BroadcastReceiver
    {
        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == UsbManager.ActionUsbDeviceAttached)
            {
                try
                {
                    var port =
                        await FindDriversService.GetUsbSerialPortAsync(
                            App.UsbManager);
                    App.PortInfo = new UsbSerialPortInfo(port);
                    var permissionGranted =
                        await App.UsbManager.RequestPermissionAsync(
                            port.Driver.Device,
                            Application.Context);
                    ToastService.ToastShortMessage(permissionGranted
                        ? AppResources.Known_Device
                        : AppResources.No_Permission);
                }
                catch (Exception)
                {
                    ToastService.ToastShortMessage(AppResources.Unknown_Device);
                }
            }
            else if (intent.Action == UsbManager.ActionUsbDeviceDetached)
            {
                App.PortInfo = null;
                MessagingCenter.Send<object>(this, "DeviceDetached");
                ToastService.ToastShortMessage(AppResources.Device_Detached);
            }
        }
    }
}