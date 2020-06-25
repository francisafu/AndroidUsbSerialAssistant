using Android.Hardware.Usb;
using AndroidUsbSerialDriver.Driver.SettingsEnum;

namespace AndroidUsbSerialDriver.Driver.UsbSerialPort
{
    public abstract class CommonUsbSerialPort : UsbSerialPort
    {
        public static int DEFAULT_READ_BUFFER_SIZE = 16 * 1024;
        public static int DEFAULT_WRITE_BUFFER_SIZE = 16 * 1024;

        // non-null when open()
        protected UsbDeviceConnection mConnection = null;

        protected UsbDevice mDevice;
        protected int mPortNumber;

        /**
         * Internal read buffer.  Guarded by {@link #mReadBufferLock}.
         */
        protected byte[] mReadBuffer;

        protected object mReadBufferLock = new object();

        /**
         * Internal write buffer.  Guarded by {@link #mWriteBufferLock}.
         */
        protected byte[] mWriteBuffer;

        protected object mWriteBufferLock = new object();

        protected CommonUsbSerialPort(UsbDevice device, int portNumber)
        {
            mDevice = device;
            mPortNumber = portNumber;

            mReadBuffer = new byte[DEFAULT_READ_BUFFER_SIZE];
            mWriteBuffer = new byte[DEFAULT_WRITE_BUFFER_SIZE];
        }

        // check if connection is still available
        public bool HasConnection => mConnection != null;

        public override string ToString()
        {
            return
                $"<{GetType().Name} device_name={mDevice.DeviceName} device_id={mDevice.DeviceId} port_number={mPortNumber}>";
        }

        /**
         * Returns the currently-bound USB device.
         * 
         * @return the device
         */
        public UsbDevice GetDevice()
        {
            return mDevice;
        }

        public override int GetPortNumber()
        {
            return mPortNumber;
        }

        /**
         * Returns the device serial number
         * @return serial number
         */
        public override string GetSerial()
        {
            return mConnection.Serial;
        }

        /**
         * Sets the size of the internal buffer used to exchange data with the USB
         * stack for read operations.  Most users should not need to change this.
         * 
         * @param bufferSize the size in bytes
         */
        public void SetReadBufferSize(int bufferSize)
        {
            lock (mReadBufferLock)
            {
                if (bufferSize == mReadBuffer.Length) return;
                mReadBuffer = new byte[bufferSize];
            }
        }

        /**
         * Sets the size of the internal buffer used to exchange data with the USB
         * stack for write operations.  Most users should not need to change this.
         * 
         * @param bufferSize the size in bytes
         */
        public void SetWriteBufferSize(int bufferSize)
        {
            lock (mWriteBufferLock)
            {
                if (bufferSize == mWriteBuffer.Length) return;
                mWriteBuffer = new byte[bufferSize];
            }
        }

        public abstract override void Open(UsbDeviceConnection connection);

        public abstract override void Close();

        public abstract override int Read(byte[] dest, int timeoutMillis);

        public abstract override int Write(byte[] src, int timeoutMillis);

        public abstract override void SetParameters(int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity);

        public abstract override bool GetCD();

        public abstract override bool GetCTS();

        public abstract override bool GetDSR();

        public abstract override bool GetDTR();

        public abstract override void SetDTR(bool value);

        public abstract override bool GetRI();

        public abstract override bool GetRTS();

        public abstract override void SetRTS(bool value);

        public override bool PurgeHwBuffers(bool flushReadBuffers,
            bool flushWriteBuffers)
        {
            return !flushReadBuffers && !flushWriteBuffers;
        }
    }
}