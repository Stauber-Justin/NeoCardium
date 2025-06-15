using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NeoCardium.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel { get; } = new SettingsPageViewModel();

        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;

            if (Enum.TryParse<ElementTheme>(ViewModel.SelectedTheme, out var theme) && App._mainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme;
            }
        }
    }
}
