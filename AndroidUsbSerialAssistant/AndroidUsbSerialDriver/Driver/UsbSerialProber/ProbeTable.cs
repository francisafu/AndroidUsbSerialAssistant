using System;
using System.Collections.Generic;
using Android.Util;

namespace AndroidUsbSerialDriver.Driver.UsbSerialProber
{
    public class ProbeTable
    {
        private readonly Dictionary<Tuple<int, int>, Type> mProbeTable =
            new Dictionary<Tuple<int, int>, Type>();

        private readonly string TAG = nameof(ProbeTable);


        /**
         * Adds or updates a (vendor, product) pair in the table.
         * 
         * @param vendorId the USB vendor id
         * @param productId the USB product id
         * @param driverClass the driver class responsible for this pair
         * @return {@code this}, for chaining
         */
        public ProbeTable AddProduct(int vendorId,
            int productId,
            Type driverClass)
        {
            var key = new Tuple<int, int>(vendorId, productId);

            if (!mProbeTable.ContainsKey(key))
                mProbeTable.Add(key, driverClass);

            return this;
        }

        public ProbeTable AddDriver(Type driverClass)
        {
            var m = driverClass.GetMethod("GetSupportedDevices");

            var devices = (Dictionary<int, int[]>) m.Invoke(null, null);

            foreach (var vendorId in devices.Keys)
            {
                var productIds = devices[vendorId];

                foreach (var productId in productIds)
                    try
                    {
                        AddProduct(vendorId, productId, driverClass);
                        Log.Debug(TAG,
                            $"Added {vendorId:X}, {productId:X}, {driverClass}");
                    }
                    catch (Exception)
                    {
                        Log.Debug(TAG,
                            $"Error adding {vendorId:X}, {productId:X}, {driverClass}");

                        throw;
                    }
            }

            return this;
        }

        public Type FindDriver(int vendorId, int productId)
        {
            var pair = new Tuple<int, int>(vendorId, productId);

            return mProbeTable.ContainsKey(pair) ? mProbeTable[pair] : null;
        }
    }
}