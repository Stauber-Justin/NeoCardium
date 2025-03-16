using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.ViewModels;

namespace NeoCardium.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel { get; } = new SettingsPageViewModel();

        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void ThemeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsDarkTheme)
            {
                this.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                this.RequestedTheme = ElementTheme.Light;
            }
            // Future: persist user preference and update global resources.
        }
    }
}
