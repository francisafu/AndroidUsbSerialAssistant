using System;
using System.Threading.Tasks;
using AndroidUsbSerialAssistant.Database;
using AndroidUsbSerialAssistant.Models;
using AndroidUsbSerialAssistant.Resx;
using AndroidUsbSerialAssistant.Services;
using Xamarin.Forms;

namespace AndroidUsbSerialAssistant.ViewModels.Navigation
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Constructor

        public SettingsViewModel()
        {
            InitialSettings();
        }

        #endregion

        #region Methods

        private async void InitialSettings()
        {
            CurrentSettings = await SETTINGS_STORE.GetAsync(1);
            if (CurrentSettings == null)
                MessagingCenter.Subscribe<object>(this,
                    "DatabaseInitialized",
                    async sender =>
                    {
                        CurrentSettings = await SETTINGS_STORE.GetAsync(1);
                        if (CurrentSettings != null)
                        {
                            MessagingCenter.Unsubscribe<object>(this,
                                "DatabaseInitialized");
                            NotifyPropertyChanged(nameof(CurrentSettings));
                        }
                    });
        }

        private async Task SaveSettings()
        {
            try
            {
                await SETTINGS_STORE.SaveAsync(CurrentSettings);
                MessagingCenter.Send<object>(this, "SettingsUpdated");
                ToastService.ToastShortMessage(AppResources.Save_Success);
            }
            catch (Exception)
            {
                ToastService.ToastShortMessage(AppResources.Save_Failed);
            }
        }

        #endregion

        #region Fields

        private Command saveSettingsCommand;

        private static readonly SqliteSettingsStore SETTINGS_STORE =
            new SqliteSettingsStore(App.Database);

        private Settings _currentSettings;

        #endregion

        #region Property

        public Settings CurrentSettings
        {
            get => _currentSettings;
            set => Set(ref _currentSettings, value);
        }

        public Command SaveSettingsCommand =>
            saveSettingsCommand
            ?? (saveSettingsCommand = new Command(async () => { await SaveSettings(); }));

        #endregion
    }
}