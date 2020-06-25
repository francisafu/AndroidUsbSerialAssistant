using System.Collections.Generic;
using Android.Hardware.Usb;
using AndroidUsbSerialDriver.Driver.Interface;

namespace AndroidUsbSerialDriver.Driver
{
    public class UsbSerialDriver : IUsbSerialDriver
    {
        protected UsbDevice mDevice;
        protected UsbSerialPort.UsbSerialPort mPort;
        public UsbDevice Device => GetDevice();

        public List<UsbSerialPort.UsbSerialPort> Ports => GetPorts();

        public UsbDevice GetDevice()
        {
            return mDevice;
        }

        public List<UsbSerialPort.UsbSerialPort> GetPorts()
        {
            return new List<UsbSerialPort.UsbSerialPort> {mPort};
        }
    }
}