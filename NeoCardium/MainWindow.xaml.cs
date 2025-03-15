using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT.Interop;
using NeoCardium.Views;
using Microsoft.UI;
using Windows.UI.Composition.Desktop;
using System;
using AppWindowType = Microsoft.UI.Windowing.AppWindow;

namespace NeoCardium
{
    /// <summary>
    /// MainWindow serves as the global navigation shell.
    /// It hosts a NavigationView that routes to CategoryPage, PracticePage, and SettingsPage.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MicaController? micaController;
        private SystemBackdropConfiguration? backdropConfig;

        public MainWindow()
        {
            this.InitializeComponent();
            EnableMica();

            // Navigate to the CategoryPage by default.
            ContentFrame.Navigate(typeof(CategoryPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag)
                {
                    case "CategoryPage":
                        ContentFrame.Navigate(typeof(CategoryPage));
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
            if (!MicaController.IsSupported()) return; // Fallback to Acrylic if not available

            micaController = new MicaController();
            backdropConfig = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default
            };

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindowType appWindow = AppWindowType.GetFromWindowId(windowId);
            var compositor = this.Content as Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop;

            if (compositor != null)
            {
                micaController.AddSystemBackdropTarget(compositor);
                micaController.SetSystemBackdropConfiguration(backdropConfig);
            }

            this.Activated += (s, e) => backdropConfig.IsInputActive = true;
            this.Closed += (s, e) =>
            {
                micaController?.Dispose();
                micaController = null;
            };
        }
    }
}
