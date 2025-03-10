using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using WinRT.Interop;

using NeoCardium.Views;
using Windows.UI.ApplicationSettings;
using Microsoft.UI;
using Windows.UI.Composition.Desktop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NeoCardium
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MicaController? micaController;
        private SystemBackdropConfiguration? backdropConfig;

        public MainWindow()
        {
            this.InitializeComponent();
            EnableMica();

            // Startet die App mit der Startseite
            ContentFrame.Navigate(typeof(MainPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag)
                {
                    case "MainPage":
                        ContentFrame.Navigate(typeof(MainPage));
                        break;
                    case "PracticePage":
                        ContentFrame.Navigate(typeof(PracticePage));
                        break;
                    case "SettingsPage":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }

        private void EnableMica()
        {
            if (!MicaController.IsSupported()) return; // Fallback zu Acrylic, falls nicht verfügbar

            // Compositor holen & SystemBackdrop aktivieren
            micaController = new MicaController();
            backdropConfig = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default
            };

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            var compositor = this.Content as Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop; // Richtige Konvertierung

            if (compositor != null)
            {
                micaController.AddSystemBackdropTarget(compositor);
                micaController.SetSystemBackdropConfiguration(backdropConfig);
            }

            // Fenster-Events für Aktivierung & Cleanup
            this.Activated += (s, e) => backdropConfig.IsInputActive = true;
            this.Closed += (s, e) =>
            {
                micaController?.Dispose();
                micaController = null;
            };
        }
    }
}
