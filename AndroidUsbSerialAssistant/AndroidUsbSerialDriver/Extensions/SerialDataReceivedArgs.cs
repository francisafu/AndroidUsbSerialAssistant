using System;

namespace AndroidUsbSerialDriver.Extensions
{
    public class SerialDataReceivedArgs : EventArgs
    {
        public SerialDataReceivedArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}