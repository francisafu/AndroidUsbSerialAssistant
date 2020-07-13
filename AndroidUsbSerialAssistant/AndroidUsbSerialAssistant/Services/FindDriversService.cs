using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Hardware.Usb;
using AndroidUsbSerialDriver.Driver;
using AndroidUsbSerialDriver.Driver.Interface;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Driver.UsbSerialProber;
using AndroidUsbSerialDriver.Extensions;

namespace AndroidUsbSerialAssistant.Services
{
    public class FindDriversService
    {
        public static Task<IList<IUsbSerialDriver>> FindAllDriversAsync(
            UsbManager usbManager)
        {
            var table = UsbSerialProber.DefaultProbeTable;

            // Adding a custom driver to the default probe table
            table.AddProduct(0x1b4f,
                0x0008,
                typeof(CdcAcmSerialDriver)); // IOIO OTG

            table.AddProduct(0x09D8,
                0x0420,
                typeof(CdcAcmSerialDriver)); // Elatec TWN4

            var prober = new UsbSerialProber(table);
            return prober.FindAllDriversAsync(usbManager);
        }

        public static async Task<UsbSerialPort> GetUsbSerialPortAsync(
            UsbManager usbManager)
        {
            var drivers = await FindAllDriversAsync(usbManager);
            var ports = drivers.SelectMany(driver => driver.Ports).ToList();
            //TODO: List All Available Ports
            return ports[0];
        }

        public static async Task<IUsbSerialDriver> GetSpecificDriverAsync(
            UsbManager usbManager,
            UsbSerialPortInfo portInfo)
        {
            var drivers = await FindAllDriversAsync(usbManager);
            var driver = drivers.FirstOrDefault(d =>
                d.Device.VendorId == portInfo.VendorId
                && d.Device.DeviceId == portInfo.DeviceId);
            return driver;
        }
    }
}