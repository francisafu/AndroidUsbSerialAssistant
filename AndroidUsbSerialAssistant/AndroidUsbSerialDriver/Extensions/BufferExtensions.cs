using System;
using Android.Runtime;
using Java.Nio;
using Byte = Java.Lang.Byte;
using Object = Java.Lang.Object;

namespace AndroidUsbSerialDriver.Extensions
{
    /// <summary>
    ///     Work around for faulty JNI wrapping in Xamarin library.  Fixes a bug
    ///     where binding for Java.Nio.ByteBuffer.Get(byte[], int, int) allocates a new temporary
    ///     Java byte array on every call
    ///     See https://bugzilla.xamarin.com/show_bug.cgi?id=31260
    ///     and http://stackoverflow.com/questions/30268400/xamarin-implementation-of-bytebuffer-get-wrong
    /// </summary>
    public static class BufferExtensions
    {
        private static IntPtr _byteBufferClassRef;
        private static IntPtr _byteBufferGetBii;

        public static ByteBuffer Get(this ByteBuffer buffer,
            JavaArray<Byte> dst,
            int dstOffset,
            int byteCount)
        {
            if (_byteBufferClassRef == IntPtr.Zero)
                _byteBufferClassRef = JNIEnv.FindClass("java/nio/ByteBuffer");
            if (_byteBufferGetBii == IntPtr.Zero)
                _byteBufferGetBii = JNIEnv.GetMethodID(_byteBufferClassRef,
                    "get",
                    "([BII)Ljava/nio/ByteBuffer;");

            return Object.GetObject<ByteBuffer>(
                JNIEnv.CallObjectMethod(buffer.Handle,
                    _byteBufferGetBii,
                    new JValue(dst),
                    new JValue(dstOffset),
                    new JValue(byteCount)),
                JniHandleOwnership.TransferLocalRef);
        }

        public static byte[] ToByteArray(this ByteBuffer buffer)
        {
            var classHandle = JNIEnv.FindClass("java/nio/ByteBuffer");
            var methodId = JNIEnv.GetMethodID(classHandle, "array", "()[B");
            var resultHandle = JNIEnv.CallObjectMethod(buffer.Handle, methodId);

            var result = JNIEnv.GetArray<byte>(resultHandle);

            JNIEnv.DeleteLocalRef(resultHandle);

            return result;
        }
    }
}