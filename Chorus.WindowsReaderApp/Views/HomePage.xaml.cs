using Chorus.WindowsReader.Common;
using Chorus.WindowsReader.Common.Logger;
using Chorus.WindowsReaderApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Chorus.WindowsReaderApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private DispatcherTimer countTimer;
        private readonly IChorusLogger<HomePageViewModel> _logger;
        public HomePageViewModel ViewModel { get; set; }

        public HomePage()
        {
            this.InitializeComponent();
            _logger = App.ServiceProvider.GetRequiredService<IChorusLogger<HomePageViewModel>>();
            ViewModel = new HomePageViewModel(_logger, Dispatcher);
            this.DataContext = ViewModel;
            this.Loaded += HomePage_Loaded;
        }

        /// <summary>
        /// Event handler for the page loaded event.
        /// Initiates an asynchronous operation to start scanning using the ViewModel.
        /// </summary>
        /// <param name="sender">The sender object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the loaded event.</param>
        private void HomePage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            countTimer = new DispatcherTimer();
            countTimer.Interval = TimeSpan.FromSeconds(GlobalHelper.AppSettings.AppKillTime);
            countTimer.Tick += CountTimer_Tick;
            countTimer.Start();
        }

        /// <summary>
        /// Count Timer for Killing application as per AppKillTime settings.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private async void CountTimer_Tick(object sender, object e)
        {
            try
            {
                if (GlobalHelper.AppSettings.IsAutoKillRequired)
                {
                    countTimer.Stop(); 
                    _logger.LogInfo($"Killing Application as IsAutoKillRequired is true.");
                    CoreApplication.Exit();
                }
                else
                {
                    await ViewModel.StartScan();
                }
            }
            catch (Exception Ex)
            {
                _logger.LogError("Error occurred during CountTimer_Tick execution", Ex);
            }
        }

        /// <summary>
        /// Event handler for text changes in the filter input field.
        /// Calls ViewModel method to apply filters based on the updated text.
        /// </summary>
        /// <param name="sender">The sender object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the text change.</param>
        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ApplyFilters();
        }

        /// <summary>
        /// Handles the double-tap event on a UI element to open tag details asynchronously.
        /// Retrieves the raw data associated with the tapped element and passes it to the ViewModel.
        /// </summary>
        /// <param name="sender">The sender object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the event.</param>
        private void OpenTagDetails(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            ViewModel.TagDetailsOpenAsync();
        }

        /// <summary>
        /// Handles the double-tap event on a ListView item to open details asynchronously.
        /// Retrieves the selected payload and associated string, if available, from the sender.
        /// </summary>
        /// <param name="sender">The ListView triggering the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void OpenPayloadDetails(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            await ViewModel.OpenPayloadDetailsAsync();
        }
    }
}
