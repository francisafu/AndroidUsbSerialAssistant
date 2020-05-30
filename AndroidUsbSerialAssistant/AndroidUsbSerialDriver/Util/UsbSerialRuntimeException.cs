using System;
using Android.Runtime;
using Java.Lang;

namespace AndroidUsbSerialDriver.Util
{
    public class UsbSerialRuntimeException : RuntimeException
    {
        public UsbSerialRuntimeException() { }

        public UsbSerialRuntimeException(Throwable throwable) : base(throwable)
        {
        }

        public UsbSerialRuntimeException(string detailMessage) : base(
            detailMessage)
        {
        }

        public UsbSerialRuntimeException(string detailMessage,
            Throwable throwable) : base(detailMessage, throwable)
        {
        }

        protected UsbSerialRuntimeException(IntPtr javaReference,
            JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
    }
}