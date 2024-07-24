using System;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Chorus.WindowsReader.Common.Helpers
{
    public static class MessageDialogService
    {
        /// <summary>
        /// Displays a message dialog asynchronously with the specified content and optional title.
        /// </summary>
        /// <param name="content">The content of the message dialog.</param>
        /// <param name="title">Optional title of the message dialog (default is an empty string).</param>
        public static async Task ShowMessageAsync(string content, string title = "")
        {
            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand("OK"));
            await dialog.ShowAsync();
        }
        /// Displays a confirmation dialog asynchronously with the specified content and optional title.
        /// Returns true if the user selects 'Yes', and false if 'No' is selected.
        /// </summary>
        /// <param name="content">The content of the confirmation dialog.</param>
        /// <param name="title">Optional title of the confirmation dialog (default is an empty string).</param>
        /// <returns>True if the user selects 'Yes'; false if 'No'.</returns>
        public static async Task<bool> ShowConfirmationAsync(string content, string title = "")
        {
            var dialog = new MessageDialog(content, title);
            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new UICommand("No") { Id = 1 });

            var result = await dialog.ShowAsync();
            return (int)result.Id == 0;
        }
    }
}
