using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NeoCardium.Helpers;
using NeoCardium.Database;
using Windows.Storage;

namespace NeoCardium
{
    public partial class App : Application
    {
        public static MainWindow? _mainWindow { get; private set; }
        private const string FirstLaunchKey = "FirstLaunchShown";
        
        private static readonly ResourceDictionary _pastelDictionary = new()
        {
            Source = new Uri("ms-appx:///Themes/Pastel.xaml")
        };

        public static void ApplyTheme(string theme)
        {
            if (_mainWindow?.Content is FrameworkElement root)
            {
                if (theme == "Pastel")
                {
                    root.RequestedTheme = ElementTheme.Light;
                    if (!Current.Resources.MergedDictionaries.Contains(_pastelDictionary))
                    {
                        Current.Resources.MergedDictionaries.Add(_pastelDictionary);
                    }
                }
                else
                {
                    if (Enum.TryParse<ElementTheme>(theme, out var etheme))
                    {
                        root.RequestedTheme = etheme;
                    }
                    Current.Resources.MergedDictionaries.Remove(_pastelDictionary);
                }
            }
        }

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

            var localSettings = ApplicationData.Current.LocalSettings;
            bool isFirstLaunch = false;
            if (!localSettings.Values.ContainsKey(FirstLaunchKey))
            {
                isFirstLaunch = true;
                localSettings.Values[FirstLaunchKey] = true;
            }

            if (isFirstLaunch)
            {
                _mainWindow.NavigateToTutorial();
            }
        }
    }
}
