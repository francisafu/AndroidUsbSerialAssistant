using System;
using System.ComponentModel;
using Xamarin.Forms.Internals;

namespace AndroidUsbSerialAssistant.Models
{
    /// <summary>
    ///     Model for chat message
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ChatMessage : INotifyPropertyChanged
    {
        #region Event

        /// <summary>
        ///     The declaration of property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        ///     The PropertyChanged event occurs when property value is changed.
        /// </summary>
        /// <param name="property">property name</param>
        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(property));
        }

        #endregion

        #region Fields

        private string message;

        private DateTime time;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        public string Message
        {
            get => message;
            set
            {
                message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        /// <summary>
        ///     Gets or sets the message sent/received time.
        /// </summary>
        public DateTime Time
        {
            get => time;
            set
            {
                time = value;
                OnPropertyChanged(nameof(Time));
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the message is received or sent.
        /// </summary>
        public bool IsReceived { get; set; }

        #endregion
    }
}