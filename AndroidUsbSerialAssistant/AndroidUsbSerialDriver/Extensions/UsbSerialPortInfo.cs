using Android.OS;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using Java.Interop;
using Java.Lang;

namespace AndroidUsbSerialDriver.Extensions
{
    public sealed class UsbSerialPortInfo : Object, IParcelable
    {
        private static readonly IParcelableCreator CREATOR =
            new ParcelableCreator();

        public UsbSerialPortInfo()
        {
        }

        public UsbSerialPortInfo(UsbSerialPort port)
        {
            var device = port.Driver.Device;
            VendorId = device.VendorId;
            DeviceId = device.DeviceId;
            PortNumber = port.PortNumber;
        }

        private UsbSerialPortInfo(Parcel parcel)
        {
            VendorId = parcel.ReadInt();
            DeviceId = parcel.ReadInt();
            PortNumber = parcel.ReadInt();
        }

        public int VendorId { get; set; }

        public int DeviceId { get; set; }

        public int PortNumber { get; set; }

        [ExportField("CREATOR")]
        public static IParcelableCreator GetCreator()
        {
            return CREATOR;
        }

        #region ParcelableCreator implementation

        public sealed class ParcelableCreator : Object, IParcelableCreator
        {
            #region IParcelableCreator implementation

            public Object CreateFromParcel(Parcel parcel)
            {
                return new UsbSerialPortInfo(parcel);
            }

            public Object[] NewArray(int size)
            {
                return new UsbSerialPortInfo[size];
            }

            #endregion
        }

        #endregion

        #region IParcelable implementation

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteInt(VendorId);
            dest.WriteInt(DeviceId);
            dest.WriteInt(PortNumber);
        }

        #endregion
    }
}