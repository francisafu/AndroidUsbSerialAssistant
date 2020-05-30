using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace AndroidUsbSerialAssistant.Views.Navigation
{
    [Preserve(AllMembers = true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WorkspacePage : ContentPage
    {
        public WorkspacePage()
        {
            InitializeComponent();
            MessagingCenter.Subscribe<object>(this,
                "DataReceived",
                sender =>
                {
                    ReceivedDataScrollView.ScrollToAsync(ReceivedDataLabel,
                        ScrollToPosition.End,
                        true);
                });
        }
    }
}