# AndroidUsbSerialAssistant

This is a driver library together with a fully functions application to allow your Xamarin app to communicate with many common USB serial hardware.  It uses the [Android USB Host API](http://developer.android.com/guide/topics/connectivity/usb/host.html)
available on Android 4.4+.

No root access, ADK, or special kernel drivers are required; all drivers are implemented in
C#.  You get a raw serial port with `Read()`, `Write()`, and other basic
functions for use with your own protocols.  The appropriate driver is picked based on the device's Vendor ID and Product ID.

This is a Xamarin C# version library port from Mike Wakerly's Java [usb-serial-for-android](https://github.com/mik3y/usb-serial-for-android) library.  It follows that library very closely.  The main changes were to make the method names follow C# standard naming conventions.  Some Java specific data types were replaced with .NET types and the reflection code is .NET specific.  Code examples written for the Java version of the library were translate more or less faithfully to C#.

It also includes code derived from LusoVU's [XamarinUsbSerial](https://bitbucket.org/lusovu/xamarinusbserial) and anotherlab's [UsbSerialForAndroid](https://github.com/anotherlab/UsbSerialForAndroid) library.  XamarinUsbSerial was a C# wrapper for the Java usb-serial-for-android.  It used an older version of the usb-serial-for-android .jar file.UsbSerialForAndroid is a 100% C# port of the original java code.

## Structure

This solution contains three projects.

* AndroidUsbSerialDriver - A port of the Java library usb-serial-for-android
* AndroidUsbSerialAssistant - A Xamarin.Form PCL example app
* AndroidUsbSerialAssistant.Android - A Xamarin.Form Android example app

## Getting Started
1. Reference the **AndroidUsbSerialDriver** library to your project

2. Copy the **device_filter.xml** from the example app to your Resources/xml folder.  Make sure that the Build Action is set to *AndroidResource*

3. Add the following attribute to the main activity to enable the USB Host
```C#
[assembly: UsesFeature("android.hardware.usb.host")]
```

4. Add the following IntentFilter to the main activity to receive USB device attached notifications
```C#
[IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
```

5. Add the MetaData attribute to associate the device_filter with the USB attached event to only see the devices that we are looking for
```C#
[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
```

6. Refer to the example app to see how connect to a serial device and read or write data with it.

## Working with unrecognized devices
The UsbSerialForAndroid has been compiled with the Vendor ID/Product ID pairs for many common serial devices.  If you have a device that is not defined by the library, but will work with one of the drivers, you can manually add the VID/PID pair.

UsbSerialProber is a class to help you find and instantiate compatible
UsbSerialDrivers from the tree of connected UsbDevices.  Normally, you will use
the default prober returned by ``UsbSerialProber.getDefaultProber()``, which
uses the built-in list of well-known VIDs and PIDs that are supported by our
drivers.

To use your own set of rules, create and use a custom prober:

```C#
// Probe for our custom CDC devices, which use VID 0x1234
// and PIDS 0x0001 and 0x0002.
var table = UsbSerialProber.DefaultProbeTable;
table.AddProduct(0x1b4f, 0x0008, typeof(CdcAcmSerialDriver)); // IOIO OTG

table.AddProduct(0x09D8, 0x0420, typeof(CdcAcmSerialDriver)); // Elatec TWN4

var prober = new UsbSerialProber(table);
List<UsbSerialDriver> drivers = prober.FindAllDrivers(usbManager);
// ...
```

Of course, nothing requires you to use UsbSerialProber at all: you can
instantiate driver classes directly if you know what you're doing; just supply
a compatible UsbDevice.


## Compatible Devices

* *Serial chips: CDCACM, Ch34x, CP21xx, FTDI, PL23xx, STM32.*

## Additional information

This is a port of the usb-serial-for-android library and code examples written for it can be adapted to C# without much effort.

For common problems, see the
[Troubleshooting](https://github.com/mik3y/usb-serial-for-android/wiki/Troubleshooting)
wiki page for usb-serial-for-android library.

For other help and discussion, please join the usb-serial-for-android Google Group,
[usb-serial-for-android](https://groups.google.com/forum/?fromgroups#!forum/usb-serial-for-android).

## License, and Copyright

This library is licensed under LGPL Version 2.1. Please see LICENSE.txt for the complete license.

Copyright 2020, Francis Fu.  All Rights Reserved.  Portions of this library are based on the [usb-serial-for-android](https://github.com/mik3y/usb-serial-for-android), [XamarinUsbSerial](https://bitbucket.org/lusovu/xamarinusbserial) and [UsbSerialForAndroid](https://github.com/anotherlab/UsbSerialForAndroid) libraries, controls used in this project are presented by [Syncfusion](https://www.syncfusion.com/xamarin-ui-controls).  Their rights remain intact.

PS. When I was coding on this project, I found those original copyright announcements (as comments inside files) quite annoying, so I move them here. Sorry for that.

### Original project copyright announcement
```
  Copyright 2017 Tyler Technologies Inc.
 
  This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation; either version 2.1 of the License, or (at your option) any later version.
 
  This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.
 
  You should have received a copy of the GNU Lesser General Public License along with this library; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 
  Portions of this library are based on usb-serial-for-android (https://github.com/mik3y/usb-serial-for-android).
  Portions of this library are based on Xamarin USB Serial for Android (https://bitbucket.org/lusovu/xamarinusbserial).
  Portions of this library are based on UsbSerialForAndroid (https://github.com/anotherlab/UsbSerialForAndroid) 
 ```

## Author Says...
This library is under LGPL because the original libraries are base on LGPL. However, you can modify it freely for commercial or non-commercial use. All I need is a **STAR**. If this project simple your work and save your life, please give me a star. I need that for a job, Thank you.