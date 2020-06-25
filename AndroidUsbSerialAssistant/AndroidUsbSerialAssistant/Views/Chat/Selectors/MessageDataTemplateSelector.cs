using AndroidUsbSerialAssistant.Models;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace AndroidUsbSerialAssistant.Views.Chat
{
    /// <summary>
    ///     Implements the message data template selector class.
    /// </summary>
    [Preserve(AllMembers = true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public class MessageDataTemplateSelector : DataTemplateSelector
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageDataTemplateSelector" /> class.
        /// </summary>
        public MessageDataTemplateSelector()
        {
            IncomingTextTemplate =
                new DataTemplate(typeof(IncomingTextTemplate));
            OutgoingTextTemplate =
                new DataTemplate(typeof(OutgoingTextTemplate));
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Returns the incoming or outgoing text template.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="container">The bindable object</param>
        /// <returns>Returns the data template</returns>
        protected override DataTemplate OnSelectTemplate(object item,
            BindableObject container)
        {
            return ((ChatMessage) item).IsReceived ? IncomingTextTemplate : OutgoingTextTemplate;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the incoming text template.
        /// </summary>
        public DataTemplate IncomingTextTemplate { get; set; }

        /// <summary>
        ///     Gets or sets the outgoing text template.
        /// </summary>
        public DataTemplate OutgoingTextTemplate { get; set; }

        #endregion
    }
}