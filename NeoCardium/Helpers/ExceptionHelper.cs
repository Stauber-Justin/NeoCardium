using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.Helpers
{
    public static class ExceptionHelper
    {
        // Speicherort: Gleiche Struktur wie die Datenbank (neben der EXE)
        private static readonly string AppFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? throw new InvalidOperationException("Konnte den Speicherort des Programms nicht bestimmen.");

        private static readonly string LogDirectory = Path.Combine(AppFolder, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "error.log");

        private static bool _isErrorDialogOpen = false;

        private static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        /// <summary>
        /// Zeigt eine asynchrone Fehlermeldung als Dialog an und loggt sie optional.
        /// Verwenden, wenn eine Nutzerinteraktion erforderlich ist.
        /// </summary>
        public static async Task ShowErrorDialogAsync(string message, Exception? ex = null, XamlRoot? xamlRoot = null, [CallerMemberName] string caller = "")
        {
            if (_isErrorDialogOpen)
                return;

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
            catch (Exception dialogEx)
            {
                Debug.WriteLine($"❌ Fehler beim Anzeigen des Fehlerdialogs: {dialogEx.Message}");
            }
            finally
            {
                _isErrorDialogOpen = false;
                await LogErrorAsync(message, ex, caller);
            }
        }

        /// <summary>
        /// Zeigt eine nicht-blockierende Fehlermeldung in einer InfoBar an.
        /// Perfekt für Fehler in Dialogen oder UI, ohne einen weiteren Dialog zu öffnen.
        /// </summary>
        public static void ShowErrorInfoBar(
            InfoBar errorInfoBar,
            string message,
            Exception? ex = null,
            [CallerMemberName] string caller = "")
        {
            if (errorInfoBar != null)
            {
                errorInfoBar.Message = $"{message}{(ex != null ? $"\nDetails: {ex.Message}" : "")}";
                errorInfoBar.IsOpen = true;
                errorInfoBar.Severity = InfoBarSeverity.Error;
            }
            else
            {
                Debug.WriteLine($"[ERROR] {message}");
            }

            LogError(message, ex, caller);
        }

        /// <summary>
        /// Fängt globale Fehler ab und zeigt eine Fehlermeldung.
        /// Muss in `App.xaml.cs` registriert werden!
        /// </summary>
        public static void RegisterGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    LogError("Ein schwerwiegender Fehler ist aufgetreten.", ex);
                    ShowErrorDialogAsync("Ein schwerwiegender Fehler ist aufgetreten.", ex).Wait();
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogError("Ein nicht behandelter asynchroner Fehler ist aufgetreten.", args.Exception);
                ShowErrorDialogAsync("Ein nicht behandelter asynchroner Fehler ist aufgetreten.", args.Exception).Wait();
                args.SetObserved();
            };
        }

        /// <summary>
        /// Asynchrones Logging – schreibt Logeinträge in die Log-Datei.
        /// Diese Methode fängt Fehler ab, sodass Log-Vorgänge die App nicht blockieren.
        /// </summary>
        public static async Task LogErrorAsync(string message, Exception? ex = null, [CallerMemberName] string caller = "")
        {
            // TODO: In Release-Builds könntest du Logging deaktivieren, je nach Anforderung(Einstellung).
            //if (!Debugger.IsAttached)
            //    return;

            try
            {
                EnsureLogDirectoryExists();

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Methode: {caller} | Fehler: {message}\n";
                if (ex != null)
                {
                    logEntry += $"Exception: {ex.GetType()} | Message: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
                }
                logEntry += "------------------------------------------\n";

                await File.AppendAllTextAsync(LogFilePath, logEntry);
                Debug.WriteLine(logEntry);
            }
            catch (Exception loggingEx)
            {
                // Fallback: Schreibe in die Debug-Konsole, wenn das Loggen fehlschlägt.
                Debug.WriteLine($"Fehler beim asynchronen Loggen: {loggingEx.Message}");
            }
        }

        /// <summary>
        /// Speichert eine detaillierte Fehlerbeschreibung in die Log-Datei und Debug-Konsole.
        /// </summary>
        public static void LogError(string message, Exception? ex = null, [CallerMemberName] string caller = "")
        {
            // TODO: In Release-Builds könntest du Logging deaktivieren, je nach Anforderung(Einstellung).
            //if (!Debugger.IsAttached)
            //    return;

            try
            {
            EnsureLogDirectoryExists(); // Ordner wird erstellt, falls er nicht existiert

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Methode: {caller} | Fehler: {message}\n";
            if (ex != null)
            {
                logEntry += $"Exception: {ex.GetType()} | Message: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
            }
            logEntry += "------------------------------------------\n";

            File.AppendAllText(LogFilePath, logEntry);
            Debug.WriteLine(logEntry);
            }
            catch (Exception loggingEx)
            {
                // Fallback: Schreibe in die Debug-Konsole, wenn das Loggen fehlschlägt.
                Debug.WriteLine($"Fehler beim Loggen: {loggingEx.Message}");
            }
        }
    }
}
