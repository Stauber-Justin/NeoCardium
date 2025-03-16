using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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

            // Global error handling (optional)
            ExceptionHelper.RegisterGlobalExceptionHandling();
        }

        /// <summary>
        /// Called when the application is launched.
        /// </summary>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // 1) Create & init the DB once at startup:
            var db = new Database.Database();
            db.Initialize();

            // 2) Insert debug data (categories, flashcards, etc.) if Debugger is attached:
            DebugUtility.InitializeDebugData();

            // 3) Create the main window if not already:
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Activate();
            }
        }
    }
}
