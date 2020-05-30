using System;
using SQLite;

namespace AndroidUsbSerialAssistant.Models
{
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    public class Records
    {
        #region Properties

        [PrimaryKey] [AutoIncrement] public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public DateTime TimeStamp { get; set; }

        #endregion
    }
}