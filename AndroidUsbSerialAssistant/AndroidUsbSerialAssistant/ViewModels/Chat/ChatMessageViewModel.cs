using System;
using System.Collections.ObjectModel;
using AndroidUsbSerialAssistant.Models.Chat;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace AndroidUsbSerialAssistant.ViewModels.Chat
{
    /// <summary>
    ///     ViewModel for chat message page.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ChatMessageViewModel : BaseViewModel
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChatMessageViewModel" /> class.
        /// </summary>
        public ChatMessageViewModel()
        {
            MakeVoiceCall = new Command(VoiceCallClicked);
            MakeVideoCall = new Command(VideoCallClicked);
            MenuCommand = new Command(MenuClicked);
            ShowCamera = new Command(CameraClicked);
            SendAttachment = new Command(AttachmentClicked);
            SendCommand = new Command(SendClicked);
            BackCommand = new Command(BackButtonClicked);
            ProfileCommand = new Command(ProfileClicked);

            GenerateMessageInfo();
        }

        #endregion

        #region Fields

        /// <summary>
        ///     Stores the message text in an array.
        /// </summary>
        private readonly string[] descriptions =
        {
            "Hi, can you tell me the specifications of the Dell Inspiron 5370 laptop?",
            " * Processor: Intel Core i5-8250U processor "
            + "\n"
            + " * OS: Pre-loaded Windows 10 with lifetime validity"
            + "\n"
            + " * Display: 13.3-inch FHD (1920 x 1080) LED display"
            + "\n"
            + " * Memory: 8GB DDR RAM with Intel UHD 620 Graphics"
            + "\n"
            + " * Battery: Lithium battery",
            "How much battery life does it have with wifi and without?",
            "Approximately 5 hours with wifi. About 7 hours without."
        };

        private string profileName = "Alex Russell";

        private string newMessage;

        private ObservableCollection<ChatMessage> chatMessageInfo =
            new ObservableCollection<ChatMessage>();

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the profile name.
        /// </summary>
        public string ProfileName
        {
            get => profileName;
            set
            {
                profileName = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a collection of chat messages.
        /// </summary>
        public ObservableCollection<ChatMessage> ChatMessageInfo
        {
            get => chatMessageInfo;
            set
            {
                chatMessageInfo = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a new message.
        /// </summary>
        public string NewMessage
        {
            get => newMessage;
            set
            {
                newMessage = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Commands

        /// <summary>
        ///     Gets or sets the command that is executed when the profile name is clicked.
        /// </summary>
        public Command ProfileCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the voice call button is clicked.
        /// </summary>
        public Command MakeVoiceCall { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the video call button is clicked.
        /// </summary>
        public Command MakeVideoCall { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the menu button is clicked.
        /// </summary>
        public Command MenuCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the camera button is clicked.
        /// </summary>
        public Command ShowCamera { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the attachment button is clicked.
        /// </summary>
        public Command SendAttachment { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the send button is clicked.
        /// </summary>
        public Command SendCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command that is executed when the back button is clicked.
        /// </summary>
        public Command BackCommand { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     base
        ///     Initializes a collection and add it to the message items.
        /// </summary>
        private void GenerateMessageInfo()
        {
            var currentTime = DateTime.Now;
            ChatMessageInfo = new ObservableCollection<ChatMessage>
            {
                new ChatMessage
                {
                    Message = descriptions[0],
                    Time = currentTime.AddMinutes(-2517),
                    IsReceived = true
                },
                new ChatMessage
                {
                    Message = descriptions[1],
                    Time = currentTime.AddMinutes(-2408)
                },
                new ChatMessage
                {
                    Message = descriptions[2],
                    Time = currentTime.AddMinutes(-1103),
                    IsReceived = true
                },
                new ChatMessage
                {
                    Message = descriptions[3],
                    Time = currentTime.AddMinutes(-1006)
                }
            };
        }

        /// <summary>
        ///     Invoked when the voice call button is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void VoiceCallClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the video call button is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void VideoCallClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the menu button is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void MenuClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the camera icon is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void CameraClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the attachment icon is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void AttachmentClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the send button is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void SendClicked(object obj)
        {
            if (!string.IsNullOrWhiteSpace(NewMessage))
                ChatMessageInfo.Add(new ChatMessage
                {
                    Message = NewMessage, Time = DateTime.Now
                });

            NewMessage = null;
        }

        /// <summary>
        ///     Invoked when the back button is clicked.
        /// </summary>
        /// <param name="obj">The object</param>
        private void BackButtonClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the Profile name is clicked.
        /// </summary>
        private void ProfileClicked(object obj)
        {
            // Do something
        }

        #endregion
    }
}