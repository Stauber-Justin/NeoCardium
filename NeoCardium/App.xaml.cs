using System;
using Microsoft.UI.Xaml;
using NeoCardium.Helpers;
using NeoCardium.Database;

namespace NeoCardium
{
    public partial class App : Application
    {
        public static MainWindow? _mainWindow { get; private set; }

        public App()
        {
            // Let WinUI locate assemblies, resources, etc.
            Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
            this.InitializeComponent();

#if DEBUG && !DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION
            UnhandledException += (sender, e) =>
            {
                // Log the exception details (custom logging in ExceptionHelper)
                ExceptionHelper.LogError("Unhandled exception", e.Exception);
                // Optionally, break in the debugger:
                // if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                e.Handled = true;
            };
#endif

            ExceptionHelper.RegisterGlobalExceptionHandling();
        }

        /// <summary>
        /// Called when the application is launched.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // Initialize the database and insert debug data if needed.
            var db = new Database.Database();
            db.Initialize();
            DebugUtility.InitializeDebugData();

            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Activate();
            }
        }
    }
}
