using System.Collections.Generic;
using Android.Hardware.Usb;

namespace AndroidUsbSerialDriver.Driver.Interface
{
    public interface IUsbSerialDriver
    {
        UsbDevice Device { get; }

        List<UsbSerialPort.UsbSerialPort> Ports { get; }

        UsbDevice GetDevice();
        List<UsbSerialPort.UsbSerialPort> GetPorts();

        //Dictionary<int, int[]> GetSupportedDevices();
    }
}