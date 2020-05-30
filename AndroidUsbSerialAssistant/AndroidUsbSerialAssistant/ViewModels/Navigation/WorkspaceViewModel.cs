using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AndroidUsbSerialAssistant.Database;
using AndroidUsbSerialAssistant.Resx;
using AndroidUsbSerialAssistant.Services;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Extensions;
using AndroidUsbSerialDriver.Util;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Log = Android.Util.Log;

namespace AndroidUsbSerialAssistant.ViewModels.Navigation
{
    /// <summary>
    ///     Workspace view model.
    /// </summary>
    [Preserve(AllMembers = true)]
    [DataContract]
    public class WorkspaceViewModel : BaseViewModel
    {
        public WorkspaceViewModel()
        {
            InitialSettings();
            MessagingCenter.Subscribe<object>(this,
                "SettingsUpdated",
                sender => { UpdateSettings(); });
            MessagingCenter.Subscribe<object>(this,
                "DeviceDetached",
                sender =>
                {
                    CurrentDeviceName = "";
                    NotifyPropertyChanged(CurrentDeviceName);
                });
        }

        #region Fields

        private Command startCommand;
        private Command pauseCommand;
        private Command clearReceivedCommand;
        private Command saveSentCommand;
        private Command saveReceivedCommand;
        private Command startAutoSendCommand;
        private Command stopAutoSendCommand;
        private Command manualSendCommand;

        private const int UsbWriteDataTimeOut = 200;
        private static UsbSerialPort _port;
        private static SerialInputOutputManager _serialIoManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isAutoSending;

        private static readonly StringBuilder RECEIVED_OUT_PUT =
            new StringBuilder();

        private static int _receivedDataCount;

        private static int _sentDataCount;


        private static readonly SqliteSettingsStore SETTINGS_STORE =
            new SqliteSettingsStore(App.Database);

        #endregion

        #region Properties

        private Models.Settings CurrentSettings { get; set; }

        public string ReceivedOutPut => RECEIVED_OUT_PUT.ToString();

        public string CurrentDeviceName { get; private set; }

        public string DataToSend { get; set; }

        public bool IsHex { get; set; }

        public string ReceivedDataCount =>
            $"{AppResources.Received}: {_receivedDataCount}";

        public string SentDataCount => $"{AppResources.Sent}: {_sentDataCount}";

        public string PortStatus
        {
            get
            {
                if (_serialIoManager != null && _serialIoManager.IsOpen)
                    return AppResources.Pause;

                return AppResources.Start;
            }
        }

        public Command PortCommand
        {
            get
            {
                if (_serialIoManager != null && _serialIoManager.IsOpen)
                    return PauseCommand;

                return StartCommand;
            }
        }

        private Command StartCommand =>
            startCommand ?? (startCommand = new Command(StartReceiving));

        private Command PauseCommand =>
            pauseCommand
            ?? (pauseCommand = new Command(async () =>
            {
                await PauseReceiving();
            }));

        public Command ClearReceivedCommand =>
            clearReceivedCommand
            ?? (clearReceivedCommand = new Command(ClearReceivedData));

        public Command ManualSendCommand =>
            manualSendCommand ?? (manualSendCommand = new Command(SendData));

        public string AutoSendStatus =>
            _isAutoSending ? AppResources.Stop : AppResources.Auto;

        public Command AutoSendCommand =>
            _isAutoSending ? StopAutoSendCommand : StartAutoSendCommand;

        public Command StartAutoSendCommand =>
            startAutoSendCommand
            ?? (startAutoSendCommand = new Command(AutoSendData));

        public Command StopAutoSendCommand =>
            stopAutoSendCommand
            ?? (stopAutoSendCommand = new Command(StopAutoSend));

        #endregion

        #region CommandMethods

        private async void StartReceiving()
        {
            if (App.UsbManager == null || App.PortInfo == null)
            {
                ToastService.ToastShortMessage(AppResources.No_Device);
                return;
            }

            var driver =
                await FindDriversService.GetSpecificDriverAsync(App.UsbManager,
                    App.PortInfo);
            if (driver != null)
            {
                _port = driver.Ports[App.PortInfo.PortNumber];
            }
            else
            {
                ToastService.ToastShortMessage(AppResources.No_Driver);
                return;
            }

            CurrentDeviceName = _port.GetType().Name;
            NotifyPropertyChanged(nameof(CurrentDeviceName));

            _serialIoManager = new SerialInputOutputManager(_port)
            {
                BaudRate = CurrentSettings.BaudRate,
                DataBits = CurrentSettings.DataBits,
                StopBits = CurrentSettings.StopBits,
                Parity = CurrentSettings.Parity
            };
            _serialIoManager.DataReceived += (sender, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    UpdateReceivedData(e.Data);
                });
            };
            _serialIoManager.ErrorReceived += (sender, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ToastService.ToastShortMessage(AppResources
                        .Received_Error);
                });
            };
            ToastService.ToastShortMessage(AppResources.Port_Listening);

            try
            {
                _serialIoManager.Open(App.UsbManager);
            }
            catch (Exception)
            {
                ToastService.ToastShortMessage(
                    $"{AppResources.Open_Failed}: {CurrentDeviceName}");
            }
            finally
            {
                NotifyPropertyChanged(nameof(PortCommand));
                NotifyPropertyChanged(nameof(PortStatus));
            }
        }

        private async Task PauseReceiving()
        {
            if (_serialIoManager == null || !_serialIoManager.IsOpen) return;
            try
            {
                _serialIoManager.Close();
                await Task.Run(async () =>
                {
                    while (_serialIoManager != null && _serialIoManager.IsOpen)
                        await Task.Delay(200);
                });
                ToastService.ToastShortMessage(AppResources.Port_Closed);
            }
            catch (Exception)
            {
                ToastService.ToastShortMessage(AppResources.Port_Closed_Error);
            }
            finally
            {
                NotifyPropertyChanged(nameof(PortCommand));
                NotifyPropertyChanged(nameof(PortStatus));
            }
        }

        private void ClearReceivedData()
        {
            RECEIVED_OUT_PUT.Clear();
            _receivedDataCount = 0;
            NotifyPropertyChanged(nameof(ReceivedOutPut));
            NotifyPropertyChanged(nameof(ReceivedDataCount));
        }

        private void SendData()
        {
            var stringData = DataToSend.Replace(Environment.NewLine, " ");
            WriteData(IsHex
                ? FormatConverter.HexStringToByteArray(stringData)
                : FormatConverter.StringToByteArray(stringData));
        }

        private void AutoSendData()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            cancellationToken.Register(() =>
                ToastService.ToastShortMessage(AppResources.Stop_Auto_Send));
            ToastService.ToastShortMessage(AppResources.Start_Auto_Send);
            _isAutoSending = true;
            NotifyPropertyChanged(nameof(AutoSendCommand));
            NotifyPropertyChanged(nameof(AutoSendStatus));
            Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        SendData();
                        await Task.Delay(CurrentSettings.Frequency,
                            cancellationToken);
                    }
                },
                cancellationToken);
        }

        private void StopAutoSend()
        {
            _cancellationTokenSource.Cancel();
            _isAutoSending = false;
            NotifyPropertyChanged(nameof(AutoSendCommand));
            NotifyPropertyChanged(nameof(AutoSendStatus));
        }

        #endregion

        #region Methods

        private async void UpdateSettings()
        {
            CurrentSettings = await SETTINGS_STORE.GetAsync(1);
        }

        private void InitialSettings()
        {
            UpdateSettings();
            if (CurrentSettings == null)
                MessagingCenter.Subscribe<object>(this,
                    "DatabaseInitialized",
                    sender =>
                    {
                        UpdateSettings();
                        if (CurrentSettings != null)
                            MessagingCenter.Unsubscribe<object>(this,
                                "DatabaseInitialized");
                    });
        }

        private void UpdateReceivedData(byte[] data)
        {
            Log.Info("SerialInputOutputManager", "Received");

            // Read {data.Length} bytes:\n

            var message = IsHex
                ? $"Hex: {FormatConverter.ByteArrayToHexString(data)}\n解码: {FormatConverter.ByteArrayToString(data)}\n\n"
                : $"远端: {FormatConverter.ByteArrayToString(data)}\n";
            RECEIVED_OUT_PUT.Append(message);
            _receivedDataCount++;
            NotifyPropertyChanged(nameof(ReceivedOutPut));
            NotifyPropertyChanged(nameof(ReceivedDataCount));
            MessagingCenter.Send<object>(this, "DataReceived");
        }

        private void WriteData(byte[] data)
        {
            if (_serialIoManager.IsOpen)
            {
                try
                {
                    _port.Write(data, UsbWriteDataTimeOut);
                }
                catch (Exception)
                {
                    ToastService.ToastShortMessage(AppResources.Write_Failed);
                    return;
                }

                _sentDataCount++;
                NotifyPropertyChanged(nameof(SentDataCount));
            }
        }

        #endregion
    }
}