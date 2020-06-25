using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace AndroidUsbSerialAssistant.ViewModels.Records
{
    /// <summary>
    ///     ViewModel for my address page.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class MyAddressViewModel : BaseViewModel
    {
        #region Constructor

        public MyAddressViewModel()
        {
            BackCommand = new Command(BackButtonClicked);
            EditCommand = new Command(EditButtonClicked);
            DeleteCommand = new Command(DeleteButtonClicked);
            AddCardCommand = new Command(AddCardButtonClicked);

            AddressDetails = new ObservableCollection<Models.Records>
            {
                new Models.Records
                {
                    Name = "John Doe"
                },
                new Models.Records
                {
                    Name = "John Doe"
                }
            };
        }

        #endregion

        #region Properties

        public ObservableCollection<Models.Records> AddressDetails { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Invoked when the back button clicked
        /// </summary>
        /// <param name="obj">The object</param>
        private void BackButtonClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the edit button clicked
        /// </summary>
        /// <param name="obj">The object</param>
        private void EditButtonClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the delete button clicked
        /// </summary>
        /// <param name="obj">The object</param>
        private void DeleteButtonClicked(object obj)
        {
            // Do something
        }

        /// <summary>
        ///     Invoked when the add card button clicked
        /// </summary>
        /// <param name="obj">The object</param>
        private void AddCardButtonClicked(object obj)
        {
            // Do something
        }

        #endregion

        #region Command

        /// <summary>
        ///     Gets or sets the command is executed when the back button is clicked.
        /// </summary>
        public Command BackCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command is executed when the edit button is clicked.
        /// </summary>
        public Command EditCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command is executed when the delete button is clicked.
        /// </summary>
        public Command DeleteCommand { get; set; }

        /// <summary>
        ///     Gets or sets the command is executed when the add card button is clicked.
        /// </summary>
        public Command AddCardCommand { get; set; }

        #endregion
    }
}