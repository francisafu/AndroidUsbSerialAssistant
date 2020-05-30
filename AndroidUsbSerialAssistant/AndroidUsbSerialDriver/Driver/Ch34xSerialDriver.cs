using System;
using System.Collections.Generic;
using System.IO;
using Android.Hardware.Usb;
using Android.Util;
using AndroidUsbSerialDriver.Driver.Interface;
using AndroidUsbSerialDriver.Driver.SettingsEnum;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Util;

namespace AndroidUsbSerialDriver.Driver
{
    public class Ch34xSerialDriver : UsbSerialDriver
    {
        private readonly string TAG = nameof(ProlificSerialDriver);

        public Ch34xSerialDriver(UsbDevice device)
        {
            mDevice = device;
            mPort = new Ch340SerialPort(mDevice, 0, this);
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {
                    UsbId.VENDOR_QINHENG,
                    new[] {UsbId.QINHENG_HL340, UsbId.QINHENG_CH341A}
                }
            };
        }

        public class Ch340SerialPort : CommonUsbSerialPort
        {
            private static readonly int USB_TIMEOUT_MILLIS = 5000;

            private readonly int DEFAULT_BAUD_RATE = 9600;

            private readonly IUsbSerialDriver Driver;

            private bool dtr;

            private UsbEndpoint mReadEndpoint;
            private UsbEndpoint mWriteEndpoint;
            private bool rts;

            public Ch340SerialPort(UsbDevice device,
                int portNumber,
                IUsbSerialDriver driver) : base(device, portNumber)
            {
                Driver = driver;
            }

            private string TAG => (Driver as Ch34xSerialDriver)?.TAG;

            public override IUsbSerialDriver GetDriver() { return Driver; }

            public override void Open(UsbDeviceConnection connection)
            {
                if (mConnection != null)
                    throw new IOException("Already opened.");

                mConnection = connection;
                var opened = false;
                try
                {
                    for (var i = 0; i < mDevice.InterfaceCount; i++)
                    {
                        var usbIface = mDevice.GetInterface(i);
                        if (mConnection.ClaimInterface(usbIface, true))
                            Log.Debug(TAG, "claimInterface " + i + " SUCCESS");
                        else
                            Log.Debug(TAG, "claimInterface " + i + " FAIL");
                    }

                    var dataIface =
                        mDevice.GetInterface(mDevice.InterfaceCount - 1);
                    for (var i = 0; i < dataIface.EndpointCount; i++)
                    {
                        var ep = dataIface.GetEndpoint(i);
                        if (ep.Type
                            == (UsbAddressing) UsbSupport.UsbEndpointXferBulk)
                        {
                            if (ep.Direction
                                == (UsbAddressing) UsbSupport.UsbDirIn)
                                mReadEndpoint = ep;
                            else
                                mWriteEndpoint = ep;
                        }
                    }


                    Initialize();
                    SetBaudRate(DEFAULT_BAUD_RATE);

                    opened = true;
                }
                finally
                {
                    if (!opened)
                        try
                        {
                            Close();
                        }
                        catch (IOException e)
                        {
                            // Ignore IOExceptions during close()
                        }
                }
            }

            public override void Close()
            {
                if (mConnection == null)
                    throw new IOException("Already closed");

                // TODO: nothing sent on close, maybe needed?

                try
                {
                    mConnection.Close();
                }
                finally
                {
                    mConnection = null;
                }
            }

            public override int Read(byte[] dest, int timeoutMillis)
            {
                int numBytesRead;
                lock (mReadBufferLock)
                {
                    var readAmt = Math.Min(dest.Length, mReadBuffer.Length);
                    numBytesRead = mConnection.BulkTransfer(mReadEndpoint,
                        mReadBuffer,
                        readAmt,
                        timeoutMillis);
                    if (numBytesRead < 0)
                        // This sucks: we get -1 on timeout, not 0 as preferred.
                        // We *should* use UsbRequest, except it has a bug/api oversight
                        // where there is no way to determine the number of bytes read
                        // in response :\ -- http://b.android.com/28023
                        return 0;
                    Buffer.BlockCopy(mReadBuffer, 0, dest, 0, numBytesRead);
                }

                return numBytesRead;
            }

            public override int Write(byte[] src, int timeoutMillis)
            {
                var offset = 0;

                while (offset < src.Length)
                {
                    int writeLength;
                    int amtWritten;

                    lock (mWriteBufferLock)
                    {
                        byte[] writeBuffer;

                        writeLength = Math.Min(src.Length - offset,
                            mWriteBuffer.Length);
                        if (offset == 0)
                        {
                            writeBuffer = src;
                        }
                        else
                        {
                            // bulkTransfer does not support offsets, make a copy.
                            Buffer.BlockCopy(src,
                                offset,
                                mWriteBuffer,
                                0,
                                writeLength);
                            writeBuffer = mWriteBuffer;
                        }

                        amtWritten = mConnection.BulkTransfer(mWriteEndpoint,
                            writeBuffer,
                            writeLength,
                            timeoutMillis);
                    }

                    if (amtWritten <= 0)
                        throw new IOException(
                            $"Error writing {writeLength} bytes at offset {offset} length={src.Length}");

                    Log.Debug(TAG,
                        $"Wrote amt={amtWritten} attempted={writeLength}");
                    offset += amtWritten;
                }

                return offset;
            }

            private int ControlOut(int request, int value, int index)
            {
                var REQTYPE_HOST_TO_DEVICE = 0x41;
                return mConnection.ControlTransfer(
                    (UsbAddressing) REQTYPE_HOST_TO_DEVICE,
                    request,
                    value,
                    index,
                    null,
                    0,
                    USB_TIMEOUT_MILLIS);
            }


            private int ControlIn(int request,
                int value,
                int index,
                byte[] buffer)
            {
                var REQTYPE_HOST_TO_DEVICE =
                    UsbConstants.UsbTypeVendor | UsbSupport.UsbDirIn;
                return mConnection.ControlTransfer(
                    (UsbAddressing) REQTYPE_HOST_TO_DEVICE,
                    request,
                    value,
                    index,
                    buffer,
                    buffer.Length,
                    USB_TIMEOUT_MILLIS);
            }

            private void CheckState(string msg,
                int request,
                int value,
                int[] expected)
            {
                var buffer = new byte[expected.Length];
                var ret = ControlIn(request, value, 0, buffer);

                if (ret < 0) throw new IOException($"Failed send cmd [{msg}]");

                if (ret != expected.Length)
                    throw new IOException(
                        $"Expected {expected.Length} bytes, but get {ret} [{msg}]");

                for (var i = 0; i < expected.Length; i++)
                {
                    if (expected[i] == -1) continue;

                    var current = buffer[i] & 0xff;
                    if (expected[i] != current)
                        throw new IOException(
                            $"Expected 0x{expected[i]:X} bytes, but get 0x{current:X} [ {msg} ]");
                }
            }

            private void WriteHandshakeByte()
            {
                if (ControlOut(0xa4,
                        ~((dtr ? 1 << 5 : 0) | (rts ? 1 << 6 : 0)),
                        0)
                    < 0) throw new IOException("Failed to set handshake byte");
            }

            private void Initialize()
            {
                CheckState("init #1",
                    0x5f,
                    0,
                    new[] {-1 /* 0x27, 0x30 */, 0x00});

                if (ControlOut(0xa1, 0, 0) < 0)
                    throw new IOException("init failed! #2");

                SetBaudRate(DEFAULT_BAUD_RATE);

                CheckState("init #4",
                    0x95,
                    0x2518,
                    new[] {-1 /* 0x56, c3*/, 0x00});

                if (ControlOut(0x9a, 0x2518, 0x0050) < 0)
                    throw new IOException("init failed! #5");

                CheckState("init #6", 0x95, 0x0706, new[] {0xff, 0xee});

                if (ControlOut(0xa1, 0x501f, 0xd90a) < 0)
                    throw new IOException("init failed! #7");

                SetBaudRate(DEFAULT_BAUD_RATE);

                WriteHandshakeByte();

                CheckState("init #10",
                    0x95,
                    0x0706,
                    new[] {-1 /* 0x9f, 0xff*/, 0xee});
            }

            private void SetBaudRate(int baudRate)
            {
                int[] baud =
                {
                    2400,
                    0xd901,
                    0x0038,
                    4800,
                    0x6402,
                    0x001f,
                    9600,
                    0xb202,
                    0x0013,
                    19200,
                    0xd902,
                    0x000d,
                    38400,
                    0x6403,
                    0x000a,
                    115200,
                    0xcc03,
                    0x0008
                };

                for (var i = 0; i < baud.Length / 3; i++)
                    if (baud[i * 3] == baudRate)
                    {
                        var ret = ControlOut(0x9a, 0x1312, baud[i * 3 + 1]);
                        if (ret < 0)
                            throw new IOException(
                                "Error setting baud rate. #1");
                        ret = ControlOut(0x9a, 0x0f2c, baud[i * 3 + 2]);
                        if (ret < 0)
                            throw new IOException(
                                "Error setting baud rate. #1");

                        return;
                    }


                throw new IOException(
                    "Baud rate " + baudRate + " currently not supported");
            }

            public override void SetParameters(int baudRate,
                int dataBits,
                StopBits stopBits,
                Parity parity)
            {
                SetBaudRate(baudRate);

                // TODO databit, stopbit and paraty set not implemented
            }

            public override bool GetCD() { return false; }

            public override bool GetCTS() { return false; }

            public override bool GetDSR() { return false; }

            public override bool GetDTR() { return dtr; }

            public override void SetDTR(bool value)
            {
                dtr = value;
                WriteHandshakeByte();
            }

            public override bool GetRI() { return false; }

            public override bool GetRTS() { return rts; }

            public override void SetRTS(bool value)
            {
                rts = value;
                WriteHandshakeByte();
            }

            public override bool PurgeHwBuffers(bool flushReadBuffers,
                bool flushWriteBuffers)
            {
                return true;
            }
        }
    }
}