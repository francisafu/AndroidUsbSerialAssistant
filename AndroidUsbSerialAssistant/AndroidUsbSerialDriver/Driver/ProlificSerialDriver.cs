using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.Hardware.Usb;
using Android.Util;
using AndroidUsbSerialDriver.Driver.Interface;
using AndroidUsbSerialDriver.Driver.SettingsEnum;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Util;
using Java.Lang;
using Exception = Java.Lang.Exception;
using Math = System.Math;
using Thread = System.Threading.Thread;

namespace AndroidUsbSerialDriver.Driver
{
    public class ProlificSerialDriver : UsbSerialDriver
    {
        private readonly string TAG = nameof(ProlificSerialDriver);

        public ProlificSerialDriver(UsbDevice device)
        {
            mDevice = device;
            mPort = new ProlificSerialPort(mDevice, 0, this);
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {UsbId.VENDOR_PROLIFIC, new[] {UsbId.PROLIFIC_PL2303}}
            };
        }


        private class ProlificSerialPort : CommonUsbSerialPort
        {
            private const int WRITE_ENDPOINT = 0x02;
            private const int READ_ENDPOINT = 0x83;
            private const int INTERRUPT_ENDPOINT = 0x81;
            private static readonly int USB_READ_TIMEOUT_MILLIS = 1000;
            private static readonly int USB_WRITE_TIMEOUT_MILLIS = 5000;

            private static readonly int USB_RECIP_INTERFACE = 0x01;

            private static readonly int PROLIFIC_VENDOR_READ_REQUEST = 0x01;
            private static readonly int PROLIFIC_VENDOR_WRITE_REQUEST = 0x01;

            private static readonly int PROLIFIC_VENDOR_OUT_REQTYPE =
                UsbSupport.UsbDirOut | UsbConstants.UsbTypeVendor;

            private static readonly int PROLIFIC_VENDOR_IN_REQTYPE =
                UsbSupport.UsbDirIn | UsbConstants.UsbTypeVendor;

            private static readonly int PROLIFIC_CTRL_OUT_REQTYPE =
                UsbSupport.UsbDirOut
                | UsbConstants.UsbTypeClass
                | USB_RECIP_INTERFACE;

            private static readonly int FLUSH_RX_REQUEST = 0x08;
            private static readonly int FLUSH_TX_REQUEST = 0x09;

            private static readonly int SET_LINE_REQUEST = 0x20;
            private static readonly int SET_CONTROL_REQUEST = 0x22;

            private static readonly int CONTROL_DTR = 0x01;
            private static readonly int CONTROL_RTS = 0x02;

            private static readonly int STATUS_FLAG_CD = 0x01;
            private static readonly int STATUS_FLAG_DSR = 0x02;
            private static readonly int STATUS_FLAG_RI = 0x08;
            private static readonly int STATUS_FLAG_CTS = 0x80;

            private static readonly int STATUS_BUFFER_SIZE = 10;
            private static readonly int STATUS_BYTE_IDX = 8;

            private static readonly int DEVICE_TYPE_HX = 0;
            private static readonly int DEVICE_TYPE_0 = 1;
            private static readonly int DEVICE_TYPE_1 = 2;

            private readonly IUsbSerialDriver Driver;
            private readonly object mReadStatusThreadLock = new object();

            private int mBaudRate = -1, mDataBits = -1;

            private int mControlLinesValue;

            private int mDeviceType = DEVICE_TYPE_HX;
            private UsbEndpoint mInterruptEndpoint;
            private Parity mParity = Parity.NotSet;

            private UsbEndpoint mReadEndpoint;
            private IOException mReadStatusException;
            private volatile Thread mReadStatusThread;

            private int mStatus;
            private StopBits mStopBits = StopBits.NotSet;
            private bool mStopReadStatusThread;
            private UsbEndpoint mWriteEndpoint;

            public ProlificSerialPort(UsbDevice device,
                int portNumber,
                IUsbSerialDriver driver) : base(device, portNumber)
            {
                Driver = driver;
            }

            private string TAG => (Driver as ProlificSerialDriver)?.TAG;

            public override IUsbSerialDriver GetDriver() { return Driver; }

            private byte[] InControlTransfer(int requestType,
                int request,
                int value,
                int index,
                int length)
            {
                var buffer = new byte[length];
                var result = mConnection.ControlTransfer(
                    (UsbAddressing) requestType,
                    request,
                    value,
                    index,
                    buffer,
                    length,
                    USB_READ_TIMEOUT_MILLIS);
                if (result != length)
                    throw new IOException(
                        $"ControlTransfer with value {value} failed: {result}");
                return buffer;
            }

            private void OutControlTransfer(int requestType,
                int request,
                int value,
                int index,
                byte[] data)
            {
                var length = data?.Length ?? 0;
                var result = mConnection.ControlTransfer(
                    (UsbAddressing) requestType,
                    request,
                    value,
                    index,
                    data,
                    length,
                    USB_WRITE_TIMEOUT_MILLIS);
                if (result != length)
                    throw new IOException(
                        $"ControlTransfer with value {value} failed: {result}");
            }

            private byte[] VendorIn(int value, int index, int length)
            {
                return InControlTransfer(PROLIFIC_VENDOR_IN_REQTYPE,
                    PROLIFIC_VENDOR_READ_REQUEST,
                    value,
                    index,
                    length);
            }

            private void VendorOut(int value, int index, byte[] data)
            {
                OutControlTransfer(PROLIFIC_VENDOR_OUT_REQTYPE,
                    PROLIFIC_VENDOR_WRITE_REQUEST,
                    value,
                    index,
                    data);
            }

            private void ResetDevice() { PurgeHwBuffers(true, true); }

            private void CtrlOut(int request, int value, int index, byte[] data)
            {
                OutControlTransfer(PROLIFIC_CTRL_OUT_REQTYPE,
                    request,
                    value,
                    index,
                    data);
            }

            private void DoBlackMagic()
            {
                VendorIn(0x8484, 0, 1);
                VendorOut(0x0404, 0, null);
                VendorIn(0x8484, 0, 1);
                VendorIn(0x8383, 0, 1);
                VendorIn(0x8484, 0, 1);
                VendorOut(0x0404, 1, null);
                VendorIn(0x8484, 0, 1);
                VendorIn(0x8383, 0, 1);
                VendorOut(0, 1, null);
                VendorOut(1, 0, null);
                VendorOut(2, mDeviceType == DEVICE_TYPE_HX ? 0x44 : 0x24, null);
            }

            private void SetControlLines(int newControlLinesValue)
            {
                CtrlOut(SET_CONTROL_REQUEST, newControlLinesValue, 0, null);
                mControlLinesValue = newControlLinesValue;
            }

            private void ReadStatusThreadFunction()
            {
                try
                {
                    while (!mStopReadStatusThread)
                    {
                        var buffer = new byte[STATUS_BUFFER_SIZE];
                        var readBytesCount =
                            mConnection.BulkTransfer(mInterruptEndpoint,
                                buffer,
                                STATUS_BUFFER_SIZE,
                                500);
                        if (readBytesCount > 0)
                        {
                            if (readBytesCount == STATUS_BUFFER_SIZE)
                                mStatus = buffer[STATUS_BYTE_IDX] & 0xff;
                            else
                                throw new IOException(
                                    $"Invalid CTS / DSR / CD / RI status buffer received, expected {STATUS_BUFFER_SIZE} bytes, but received {readBytesCount}");
                        }
                    }
                }
                catch (IOException e)
                {
                    mReadStatusException = e;
                }
            }

            private int GetStatus()
            {
                if (mReadStatusThread == null && mReadStatusException == null)
                    lock (mReadStatusThreadLock)
                    {
                        if (mReadStatusThread == null)
                        {
                            var buffer = new byte[STATUS_BUFFER_SIZE];
                            var readBytes =
                                mConnection.BulkTransfer(mInterruptEndpoint,
                                    buffer,
                                    STATUS_BUFFER_SIZE,
                                    100);
                            if (readBytes != STATUS_BUFFER_SIZE)
                                Log.Warn(TAG,
                                    "Could not read initial CTS / DSR / CD / RI status");
                            else
                                mStatus = buffer[STATUS_BYTE_IDX] & 0xff;

                            ThreadStart mReadStatusThreadDelegate =
                                ReadStatusThreadFunction;

                            mReadStatusThread =
                                new Thread(mReadStatusThreadDelegate);

                            mReadStatusThread.Start();

                            //mReadStatusThread = new Thread(new Runnable()
                            //{
                            //    public void run()
                            //    {
                            //        ReadStatusThreadFunction();
                            //    }
                            //});
                            //mReadStatusThread.Daemon = true;//  setDaemon(true);
                            //mReadStatusThread.Start();
                        }
                    }


                /* throw and clear an exception which occured in the status read thread */
                var readStatusException = mReadStatusException;
                if (mReadStatusException != null)
                {
                    mReadStatusException = null;
                    throw readStatusException;
                }

                return mStatus;
            }

            private bool TestStatusFlag(int flag)
            {
                return (GetStatus() & flag) == flag;
            }

            public override void Open(UsbDeviceConnection connection)
            {
                if (mConnection != null) throw new IOException("Already open");

                var usbInterface = mDevice.GetInterface(0);

                if (!connection.ClaimInterface(usbInterface, true))
                    throw new IOException(
                        "Error claiming Prolific interface 0");
                mConnection = connection;
                var opened = false;
                try
                {
                    for (var i = 0; i < usbInterface.EndpointCount; ++i)
                    {
                        var currentEndpoint = usbInterface.GetEndpoint(i);

                        switch (currentEndpoint.Address)
                        {
                            case (UsbAddressing) READ_ENDPOINT:
                                mReadEndpoint = currentEndpoint;
                                break;

                            case (UsbAddressing) WRITE_ENDPOINT:
                                mWriteEndpoint = currentEndpoint;
                                break;

                            case (UsbAddressing) INTERRUPT_ENDPOINT:
                                mInterruptEndpoint = currentEndpoint;
                                break;
                        }
                    }

                    if (mDevice.DeviceClass == (UsbClass) 0x02)
                        mDeviceType = DEVICE_TYPE_0;
                    else
                        try
                        {
                            //Method getRawDescriptorsMethod
                            //    = mConnection.getClass().getMethod("getRawDescriptors");
                            //byte[] rawDescriptors
                            //    = (byte[])getRawDescriptorsMethod.invoke(mConnection);

                            var rawDescriptors =
                                mConnection.GetRawDescriptors();

                            var maxPacketSize0 = rawDescriptors[7];
                            if (maxPacketSize0 == 64)
                            {
                                mDeviceType = DEVICE_TYPE_HX;
                            }
                            else if (mDevice.DeviceClass == 0x00
                                     || mDevice.DeviceClass == (UsbClass) 0xff)
                            {
                                mDeviceType = DEVICE_TYPE_1;
                            }
                            else
                            {
                                Log.Warn(TAG,
                                    "Could not detect PL2303 subtype, "
                                    + "Assuming that it is a HX device");
                                mDeviceType = DEVICE_TYPE_HX;
                            }
                        }
                        catch (NoSuchMethodException)
                        {
                            Log.Warn(TAG,
                                "Method UsbDeviceConnection.getRawDescriptors, "
                                + "required for PL2303 subtype detection, not "
                                + "available! Assuming that it is a HX device");
                            mDeviceType = DEVICE_TYPE_HX;
                        }
                        catch (Exception e)
                        {
                            Log.Error(TAG,
                                "An unexpected exception occured while trying "
                                + "to detect PL2303 subtype",
                                e);
                        }

                    SetControlLines(mControlLinesValue);
                    ResetDevice();

                    DoBlackMagic();
                    opened = true;
                }
                finally
                {
                    if (!opened)
                    {
                        mConnection = null;
                        connection.ReleaseInterface(usbInterface);
                    }
                }
            }

            public override void Close()
            {
                if (mConnection == null)
                    throw new IOException("Already closed");
                try
                {
                    mStopReadStatusThread = true;
                    lock (mReadStatusThreadLock)
                    {
                        if (mReadStatusThread != null)
                            try
                            {
                                mReadStatusThread.Join();
                            }
                            catch (Exception e)
                            {
                                Log.Warn(TAG,
                                    "An error occured while waiting for status read thread",
                                    e);
                            }
                    }

                    ResetDevice();
                }
                finally
                {
                    try
                    {
                        mConnection.ReleaseInterface(mDevice.GetInterface(0));
                    }
                    finally
                    {
                        mConnection = null;
                    }
                }
            }

            public override int Read(byte[] dest, int timeoutMillis)
            {
                lock (mReadBufferLock)
                {
                    // return ReadHelper(dest, timeoutMillis, 0);

                    var readAmt = Math.Min(dest.Length, mReadBuffer.Length);
                    var totalLength = 0;
                    while (true)
                    {
                        var numBytesRead =
                            mConnection.BulkTransfer(mReadEndpoint,
                                mReadBuffer,
                                readAmt,
                                timeoutMillis);
                        if (numBytesRead <= 0) return totalLength;
                        Buffer.BlockCopy(mReadBuffer,
                            0,
                            dest,
                            totalLength,
                            numBytesRead);
                        totalLength += numBytesRead;
                        Thread.Sleep(10);
                    }
                }
            }

            private int ReadHelper(byte[] dest, int timeoutMillis, int offset)
            {
                var readAmt = Math.Min(dest.Length, mReadBuffer.Length);

                var numBytesRead = mConnection.BulkTransfer(mReadEndpoint,
                    mReadBuffer,
                    readAmt,
                    timeoutMillis);
                if (numBytesRead <= 0) return 0;
                Buffer.BlockCopy(mReadBuffer, 0, dest, offset, numBytesRead);
                Thread.Sleep(10);
                return numBytesRead
                       + ReadHelper(dest, 0, offset + numBytesRead);
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

                    offset += amtWritten;
                }

                return offset;
            }

            public override void SetParameters(int baudRate,
                int dataBits,
                StopBits stopBits,
                Parity parity)
            {
                if (mBaudRate == baudRate
                    && mDataBits == dataBits
                    && mStopBits == stopBits
                    && mParity == parity)
                    // Make sure no action is performed if there is nothing to change
                    return;

                var lineRequestData = new byte[7];

                lineRequestData[0] = (byte) (baudRate & 0xff);
                lineRequestData[1] = (byte) ((baudRate >> 8) & 0xff);
                lineRequestData[2] = (byte) ((baudRate >> 16) & 0xff);
                lineRequestData[3] = (byte) ((baudRate >> 24) & 0xff);

                switch (stopBits)
                {
                    case StopBits.One:
                        lineRequestData[4] = 0;
                        break;

                    case StopBits.OnePointFive:
                        lineRequestData[4] = 1;
                        break;

                    case StopBits.Two:
                        lineRequestData[4] = 2;
                        break;

                    default:
                        throw new IllegalArgumentException(
                            "Unknown stopBits value: " + stopBits);
                }

                switch (parity)
                {
                    case Parity.None:
                        lineRequestData[5] = 0;
                        break;

                    case Parity.Odd:
                        lineRequestData[5] = 1;
                        break;

                    case Parity.Even:
                        lineRequestData[5] = 2;
                        break;

                    case Parity.Mark:
                        lineRequestData[5] = 3;
                        break;

                    case Parity.Space:
                        lineRequestData[5] = 4;
                        break;

                    default:
                        throw new IllegalArgumentException(
                            "Unknown parity value: " + parity);
                }

                lineRequestData[6] = (byte) dataBits;

                CtrlOut(SET_LINE_REQUEST, 0, 0, lineRequestData);

                ResetDevice();

                mBaudRate = baudRate;
                mDataBits = dataBits;
                mStopBits = stopBits;
                mParity = parity;
            }

            public override bool GetCD()
            {
                return TestStatusFlag(STATUS_FLAG_CD);
            }


            public override bool GetCTS()
            {
                return TestStatusFlag(STATUS_FLAG_CTS);
            }


            public override bool GetDSR()
            {
                return TestStatusFlag(STATUS_FLAG_DSR);
            }


            public override bool GetDTR()
            {
                return (mControlLinesValue & CONTROL_DTR) == CONTROL_DTR;
            }


            public override void SetDTR(bool value)
            {
                int newControlLinesValue;
                if (value)
                    newControlLinesValue = mControlLinesValue | CONTROL_DTR;
                else
                    newControlLinesValue = mControlLinesValue & ~CONTROL_DTR;
                SetControlLines(newControlLinesValue);
            }


            public override bool GetRI()
            {
                return TestStatusFlag(STATUS_FLAG_RI);
            }


            public override bool GetRTS()
            {
                return (mControlLinesValue & CONTROL_RTS) == CONTROL_RTS;
            }


            public override void SetRTS(bool value)
            {
                int newControlLinesValue;
                if (value)
                    newControlLinesValue = mControlLinesValue | CONTROL_RTS;
                else
                    newControlLinesValue = mControlLinesValue & ~CONTROL_RTS;
                SetControlLines(newControlLinesValue);
            }

            public override bool PurgeHwBuffers(bool purgeReadBuffers,
                bool purgeWriteBuffers)
            {
                if (purgeReadBuffers) VendorOut(FLUSH_RX_REQUEST, 0, null);

                if (purgeWriteBuffers) VendorOut(FLUSH_TX_REQUEST, 0, null);

                return purgeReadBuffers || purgeWriteBuffers;
            }
        }
    }
}