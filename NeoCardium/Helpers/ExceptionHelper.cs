using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.Helpers
{
    public static class ExceptionHelper
    {
        private static readonly string LogFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "error.log"
        );

        /// <summary>
        /// Zeigt eine Fehlermeldung in einem ContentDialog an und loggt sie optional.
        /// </summary>
        /// 

        private static bool _isErrorDialogOpen = false;
        public static async Task ShowErrorDialogAsync(string message, Exception? ex = null, XamlRoot? xamlRoot = null)
        {
            if (_isErrorDialogOpen)
            {
                return; // Verhindert das Öffnen mehrerer Dialoge gleichzeitig
            }

            _isErrorDialogOpen = true;

            try
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Fehler",
                    Content = $"{message}{(ex != null ? $"\n\nDetails: {ex.Message}" : "")}",
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };

                await errorDialog.ShowAsync();
            }
            finally
            {
                _isErrorDialogOpen = false;
            }
        }

        public static void ShowError(InfoBar errorInfoBar, string message)
        {
            if (errorInfoBar != null)
            {
                errorInfoBar.Message = message;
                errorInfoBar.IsOpen = true;
                errorInfoBar.Severity = InfoBarSeverity.Error;
            }
            else
            {
                Debug.WriteLine($"[ERROR] {message}");
            }
        }

        /// <summary>
        /// Speichert Fehlermeldungen in error.log.
        /// </summary>
        private static void LogError(string message, Exception ex)
        {
            string logEntry = $"{DateTime.Now}: {message}\n{ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(LogFilePath, logEntry);
        }
    }
}
