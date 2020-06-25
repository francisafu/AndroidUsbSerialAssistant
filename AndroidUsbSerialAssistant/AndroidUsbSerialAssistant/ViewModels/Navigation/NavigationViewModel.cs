using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace AndroidUsbSerialAssistant.ViewModels.Navigation
{
    /// <summary>
    ///     ViewModel for the Navigation list with cards page.
    /// </summary>
    [Preserve(AllMembers = true)]
    [DataContract]
    public class NavigationViewModel
    {
        #region Fields

        private Command<object> itemTappedCommand;

        #endregion

        #region Methods

        /// <summary>
        ///     Invoked when an item is selected from the navigation list.
        /// </summary>
        /// <param name="selectedItem">Selected item from the list view.</param>
        private void NavigateToNextPage(object selectedItem)
        {
            // Do something
        }

        #endregion

        #region Constructor

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the command that will be executed when an item is selected.
        /// </summary>
        public Command<object> ItemTappedCommand =>
            itemTappedCommand
            ?? (itemTappedCommand = new Command<object>(NavigateToNextPage));

        /// <summary>
        ///     Gets or sets a collection of values to be displayed in the Navigation list page.
        /// </summary>
        [DataMember(Name = "navigationList")]
        public ObservableCollection<NavigationModel> NavigationList { get; set; }

        #endregion
    }
}