using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using AndroidUsbSerialAssistant.Database;
using AndroidUsbSerialAssistant.Database.Interface;
using AndroidUsbSerialAssistant.Models;
using AndroidUsbSerialAssistant.Services;
using AndroidUsbSerialAssistant.Views.Navigation;
using AndroidUsbSerialDriver.Driver.SettingsEnum;
using AndroidUsbSerialDriver.Extensions;
using Xamarin.Forms;
using Application = Android.App.Application;

[assembly: UsesFeature("android.hardware.usb.host")] // To enable the USB host

namespace AndroidUsbSerialAssistant
{
    public partial class App
    {
        private static ISqliteDatabase _database;

        private static readonly SqliteSettingsStore SETTINGS_STORE =
            new SqliteSettingsStore(Database);

        private readonly UsbDeviceStatusBroadcastReceiverService _receiver =
            new UsbDeviceStatusBroadcastReceiverService();

        public App()
        {
            InitializeComponent();

            MainPage = new BottomNavigationPage();
        }

        public static ISqliteDatabase Database =>
            _database ?? (_database = new SqliteDatabase());

        public static UsbManager UsbManager { get; private set; }
        public static UsbSerialPortInfo PortInfo { get; set; } = null;


        protected override void OnStart()
        {
            base.OnStart();
            // 3~5 seconds latency when plug in the device here, and I don't know why
            Application.Context.RegisterReceiver(_receiver,
                new IntentFilter(UsbManager.ActionUsbDeviceAttached));
            Application.Context.RegisterReceiver(_receiver,
                new IntentFilter(UsbManager.ActionUsbDeviceDetached));
            UsbManager =
                Application.Context.GetSystemService(Context.UsbService) as
                    UsbManager;
            InitialDatabase();
        }

        private async void InitialDatabase()
        {
            if (await SETTINGS_STORE.GetAsync(1) == null)
            {
                await SETTINGS_STORE.SaveAsync(new Settings
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Frequency = 1000
                });
                MessagingCenter.Send<object>(this, "DatabaseInitialized");
            }
        }

        protected override void OnSleep()
        {
            Application.Context.UnregisterReceiver(_receiver);
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
            Application.Context.RegisterReceiver(_receiver,
                new IntentFilter(UsbManager.ActionUsbDeviceAttached));
            Application.Context.RegisterReceiver(_receiver,
                new IntentFilter(UsbManager.ActionUsbDeviceDetached));
        }
    }
}