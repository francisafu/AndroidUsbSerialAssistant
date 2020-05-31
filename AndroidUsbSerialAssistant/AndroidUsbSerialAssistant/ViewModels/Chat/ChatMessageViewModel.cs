using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AndroidUsbSerialAssistant.Database;
using AndroidUsbSerialAssistant.Models.Chat;
using AndroidUsbSerialAssistant.Resx;
using AndroidUsbSerialAssistant.Services;
using AndroidUsbSerialDriver.Driver.UsbSerialPort;
using AndroidUsbSerialDriver.Extensions;
using AndroidUsbSerialDriver.Util;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace AndroidUsbSerialAssistant.ViewModels.Chat
{
    [Preserve(AllMembers = true)]
    [DataContract]
    public class ChatMessageViewModel : BaseViewModel
    {
        #region Constructor

        public ChatMessageViewModel()
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
            ChatMessageCollection = new ObservableCollection<ChatMessage>();
        }

        #endregion

        #region Fields

        private Command startCommand;
        private Command pauseCommand;
        private Command clearCommand;
        private Command saveCommand;
        private Command startAutoSendCommand;
        private Command stopAutoSendCommand;
        private Command manualSendCommand;
        private Command getGpsCommand;

        private const int UsbWriteDataTimeOut = 200;
        private static UsbSerialPort _port;
        private static SerialInputOutputManager _serialIoManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isAutoSending;
        private static int _receivedDataCount;
        private static int _sentDataCount;

        private static readonly SqliteSettingsStore SETTINGS_STORE =
            new SqliteSettingsStore(App.Database);

        private string _newMessage;

        private ObservableCollection<ChatMessage> _chatMessageCollection =
            new ObservableCollection<ChatMessage>();

        #endregion

        #region Public Properties

        public string CurrentDeviceName { get; private set; }

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
       
        public Command ClearMessagesCommand =>
            clearCommand ?? (clearCommand = new Command(ClearData));

        public Command ManualSendCommand =>
            manualSendCommand
            ?? (manualSendCommand = new Command(ManualSendData));

        public Command AutoSendCommand =>
            _isAutoSending ? StopAutoSendCommand : StartAutoSendCommand;

        public ObservableCollection<ChatMessage> ChatMessageCollection
        {
            get => _chatMessageCollection;
            set
            {
                _chatMessageCollection = value;
                NotifyPropertyChanged();
            }
        }

        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsAutoSending
        {
            get => _isAutoSending;
            set
            {
                _isAutoSending = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Private Properties

        private Models.Settings CurrentSettings { get; set; }
        private Command StartCommand =>
            startCommand ?? (startCommand = new Command(StartReceiving));

        private Command PauseCommand =>
            pauseCommand
            ?? (pauseCommand = new Command(async () =>
            {
                await PauseReceiving();
            }));
        private Command StartAutoSendCommand =>
            startAutoSendCommand
            ?? (startAutoSendCommand = new Command(AutoSendData));

        private Command StopAutoSendCommand =>
            stopAutoSendCommand
            ?? (stopAutoSendCommand = new Command(StopAutoSend));

        #endregion

        #region Command Methods

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

        private void ClearData()
        {
            ChatMessageCollection.Clear();
            _receivedDataCount = 0;
            _sentDataCount = 0;
            NotifyPropertyChanged(nameof(ChatMessageCollection));
            NotifyPropertyChanged(nameof(ReceivedDataCount));
            NotifyPropertyChanged(nameof(SentDataCount));
        }

        private void ManualSendData()
        {
            if (WriteData())
            {
                ChatMessageCollection.Add(new ChatMessage
                {
                    Message = NewMessage, Time = DateTime.Now
                });
                NewMessage = null;
            }
        }

        private void AutoSendData()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            cancellationToken.Register(() =>
                ToastService.ToastShortMessage(AppResources.Stop_Auto_Send));
            ToastService.ToastShortMessage(AppResources.Start_Auto_Send);
            IsAutoSending = true;
            NotifyPropertyChanged(nameof(AutoSendCommand));
            Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested
                           && WriteData())
                    {
                        ChatMessageCollection.Add(new ChatMessage
                        {
                            Message = NewMessage, Time = DateTime.Now
                        });
                        await Task.Delay(CurrentSettings.Frequency,
                            cancellationToken);
                    }
                },
                cancellationToken);
        }

        private void StopAutoSend()
        {
            _cancellationTokenSource.Cancel();
            IsAutoSending = false;
            NewMessage = null;
            NotifyPropertyChanged(nameof(AutoSendCommand));
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
            var message = IsHex
                ? $"Hex: {FormatConverter.ByteArrayToHexString(data)}\n½âÂë: {FormatConverter.ByteArrayToString(data)}"
                : $"{FormatConverter.ByteArrayToString(data)}";
            ChatMessageCollection.Add(new ChatMessage
            {
                Message = message, Time = DateTime.Now,
                IsReceived = true
            });
            _receivedDataCount++;
            NotifyPropertyChanged(nameof(ReceivedDataCount));
        }

        private bool WriteData()
        {
            if (string.IsNullOrWhiteSpace(NewMessage)
                || !_serialIoManager.IsOpen) return false;
            var data = IsHex
                ? FormatConverter.HexStringToByteArray(
                    NewMessage.Replace(Environment.NewLine, " "))
                : FormatConverter.StringToByteArray(
                    NewMessage.Replace(Environment.NewLine, " "));

            try
            {
                _port.Write(data, UsbWriteDataTimeOut);
            }
            catch (Exception)
            {
                ToastService.ToastShortMessage(AppResources.Write_Failed);
                return false;
            }

            _sentDataCount++;
            NotifyPropertyChanged(nameof(SentDataCount));
            return true;
        }

        #endregion
    }
}