using System;
using System.Collections.Generic;
using System.IO;
using Android.Hardware.Usb;
using Android.Util;
using AndroidUsbSerialDriver.Driver.Interface;
using AndroidUsbSerialDriver.Driver.SettingsEnum;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Util;
using Java.Nio;

namespace AndroidUsbSerialDriver.Driver
{
    public class STM32SerialDriver : UsbSerialDriver
    {
        private readonly string TAG = nameof(STM32SerialDriver);

        private int mCtrlInterf;

        public STM32SerialDriver(UsbDevice device)
        {
            mDevice = device;
            mPort = new STM32SerialPort(mDevice, 0, this);
        }

        public class STM32SerialPort : CommonUsbSerialPort
        {
            private const int USB_WRITE_TIMEOUT_MILLIS = 5000;

            private const int USB_RECIP_INTERFACE = 0x01;

            private const int USB_RT_AM =
                UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;

            private const int SET_LINE_CODING = 0x20; // USB CDC 1.1 section 6.2
            private const int SET_CONTROL_LINE_STATE = 0x22;

            private readonly IUsbSerialDriver Driver;

            private readonly bool ENABLE_ASYNC_READS;
            private readonly string TAG = nameof(STM32SerialDriver);
            private UsbInterface mControlInterface;
            private UsbInterface mDataInterface;
            private bool mDtr;

            private UsbEndpoint mReadEndpoint;

            private bool mRts;
            private UsbEndpoint mWriteEndpoint;

            public STM32SerialPort(UsbDevice device,
                int portNumber,
                IUsbSerialDriver driver) : base(device, portNumber)
            {
                Driver = driver;
                ENABLE_ASYNC_READS = true;
            }

            public override IUsbSerialDriver GetDriver() { return Driver; }

            private int
                SendAcmControlMessage(int request, int value, byte[] buf)
            {
                return mConnection.ControlTransfer((UsbAddressing) USB_RT_AM,
                    request,
                    value,
                    (Driver as STM32SerialDriver).mCtrlInterf,
                    buf,
                    buf?.Length ?? 0,
                    USB_WRITE_TIMEOUT_MILLIS);
            }

            public override void Open(UsbDeviceConnection connection)
            {
                if (mConnection != null)
                    throw new IOException("Already opened.");

                mConnection = connection;
                var opened = false;
                var controlInterfaceFound = false;
                try
                {
                    for (var i = 0; i < mDevice.InterfaceCount; i++)
                    {
                        mControlInterface = mDevice.GetInterface(i);
                        if (mControlInterface.InterfaceClass == UsbClass.Comm)
                        {
                            if (!mConnection.ClaimInterface(mControlInterface,
                                true))
                                throw new IOException(
                                    "Could not claim control interface");
                            (Driver as STM32SerialDriver).mCtrlInterf = i;
                            controlInterfaceFound = true;
                            break;
                        }
                    }

                    if (!controlInterfaceFound)
                        throw new IOException(
                            "Could not claim control interface");
                    for (var i = 0; i < mDevice.InterfaceCount; i++)
                    {
                        mDataInterface = mDevice.GetInterface(i);
                        if (mDataInterface.InterfaceClass == UsbClass.CdcData)
                        {
                            if (!mConnection.ClaimInterface(mDataInterface,
                                true))
                                throw new IOException(
                                    "Could not claim data interface");
                            mReadEndpoint = mDataInterface.GetEndpoint(1);
                            mWriteEndpoint = mDataInterface.GetEndpoint(0);
                            opened = true;
                            break;
                        }
                    }

                    if (!opened)
                        throw new IOException(
                            "Could not claim data interface.");
                }
                finally
                {
                    if (!opened)
                        mConnection = null;
                }
            }

            public override void Close()
            {
                if (mConnection == null)
                    throw new IOException("Already closed");
                mConnection.Close();
                mConnection = null;
            }

            public override int Read(byte[] dest, int timeoutMillis)
            {
                if (ENABLE_ASYNC_READS)
                {
                    var request = new UsbRequest();
                    try
                    {
                        request.Initialize(mConnection, mReadEndpoint);
                        var buf = ByteBuffer.Wrap(dest);
                        if (!request.Queue(buf, dest.Length))
                            throw new IOException("Error queuing request");

                        var response = mConnection.RequestWait();
                        if (response == null)
                            throw new IOException("Null response");

                        var nread = buf.Position();
                        return nread > 0 ? nread : 0;
                    }
                    finally
                    {
                        request.Close();
                    }
                }

                int numBytesRead;
                lock (mReadBufferLock)
                {
                    var readAmt = Math.Min(dest.Length, mReadBuffer.Length);
                    numBytesRead = mConnection.BulkTransfer(mReadEndpoint,
                        mReadBuffer,
                        readAmt,
                        timeoutMillis);
                    if (numBytesRead < 0)
                    {
                        // This sucks: we get -1 on timeout, not 0 as preferred.
                        // We *should* use UsbRequest, except it has a bug/api oversight
                        // where there is no way to determine the number of bytes read
                        // in response :\ -- http://b.android.com/28023
                        if (timeoutMillis == int.MaxValue)
                            // Hack: Special case "~infinite timeout" as an error.
                            return -1;

                        return 0;
                    }

                    Array.Copy(mReadBuffer, 0, dest, 0, numBytesRead);
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
                            Array.Copy(src,
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

            public override void SetParameters(int baudRate,
                int dataBits,
                StopBits stopBits,
                Parity parity)
            {
                byte stopBitsBytes;
                switch (stopBits)
                {
                    case StopBits.One:
                        stopBitsBytes = 0;
                        break;
                    case StopBits.OnePointFive:
                        stopBitsBytes = 1;
                        break;
                    case StopBits.Two:
                        stopBitsBytes = 2;
                        break;
                    default:
                        throw new ArgumentException(
                            $"Bad value for stopBits: {stopBits}");
                }

                byte parityBitesBytes;
                switch (parity)
                {
                    case Parity.None:
                        parityBitesBytes = 0;
                        break;
                    case Parity.Odd:
                        parityBitesBytes = 1;
                        break;
                    case Parity.Even:
                        parityBitesBytes = 2;
                        break;
                    case Parity.Mark:
                        parityBitesBytes = 3;
                        break;
                    case Parity.Space:
                        parityBitesBytes = 4;
                        break;
                    default:
                        throw new ArgumentException(
                            $"Bad value for parity: {parity}");
                }

                byte[] msg =
                {
                    (byte) (baudRate & 0xff),
                    (byte) ((baudRate >> 8) & 0xff),
                    (byte) ((baudRate >> 16) & 0xff),
                    (byte) ((baudRate >> 24) & 0xff),
                    stopBitsBytes,
                    parityBitesBytes,
                    (byte) dataBits
                };
                SendAcmControlMessage(SET_LINE_CODING, 0, msg);
            }

            public override bool GetCD() { return false; } //TODO

            public override bool GetCTS() { return false; } //TODO

            public override bool GetDSR() { return false; } // TODO

            public override bool GetDTR() { return mDtr; }

            public override void SetDTR(bool value)
            {
                mDtr = value;
                SetDtrRts();
            }

            public override bool GetRI() { return false; } //TODO

            public override bool GetRTS() { return mRts; } //TODO

            public override void SetRTS(bool value)
            {
                mRts = value;
                SetDtrRts();
            }

            private void SetDtrRts()
            {
                var value = (mRts ? 0x2 : 0) | (mDtr ? 0x1 : 0);
                SendAcmControlMessage(SET_CONTROL_LINE_STATE, value, null);
            }

            public static Dictionary<int, int[]> GetSupportedDevices()
            {
                return new Dictionary<int, int[]>
                {
                    {
                        UsbId.VENDOR_STM,
                        new[] {UsbId.STM32_STLINK, UsbId.STM32_VCOM}
                    }
                };
            }
        }
    }
}