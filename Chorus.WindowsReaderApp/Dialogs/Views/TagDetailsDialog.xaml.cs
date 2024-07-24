using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Chorus.WindowsReaderApp.Dialogs.Views
{
    public sealed partial class TagDetailsDialog : ContentDialog
    {
        private ObservableCollection<string> _rawData = new ObservableCollection<string>();
        public ObservableCollection<string> RawData
        {
            get { return _rawData; }
            set { _rawData = value; }
        }

        /// <summary>
        /// Initializes a new instance of the TagDetailsDialog class.
        /// Sets up the dialog with provided raw data and a title.
        /// </summary>
        /// <param name="rawData">The list of raw data strings to display in the dialog.</param>
        /// <param name="title">The title of the dialog.</param>
        public TagDetailsDialog(List<string> rawData, string title)
        {
            this.InitializeComponent();
            if (rawData != null && rawData.Count > 0)
            {
                foreach (var item in rawData)
                {
                    RawData.Add(item);
                }
            }
            else
            {
                RawData = new ObservableCollection<string>() { "No Data Available" };
            }
            Title = title;
        }

        /// <summary>
        /// Event handler for the secondary button click in a ContentDialog.
        /// Hides the ContentDialog when the secondary button is clicked.
        /// </summary>
        /// <param name="sender">The sender object that triggered the event (a ContentDialog).</param>
        /// <param name="args">The event arguments containing information about the button click.</param>
        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Hide();
        }

        /// <summary>
        /// Event handler for item click in the dataListView.
        /// Copies the clicked item's string representation to the clipboard.
        /// </summary>
        /// <param name="sender">The sender object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the clicked item.</param>
        private void DataListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                var dp = new DataPackage() { RequestedOperation = DataPackageOperation.Copy };
                dp.SetText(e.ClickedItem.ToString());
                Clipboard.SetContent(dp);
            }
        }
    }
}
