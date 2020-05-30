using AndroidUsbSerialDriver.Driver.SettingsEnum;
using SQLite;

namespace AndroidUsbSerialAssistant.Models
{
    /// <summary>
    ///     Settings model.
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    public class Settings
    {
        #region Properties

        [PrimaryKey] [AutoIncrement] public int Id { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int Frequency { get; set; }

        #endregion
    }
}