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
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var lang = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
            if (string.IsNullOrEmpty(lang))
            {
                lang = Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault() ?? "en-US";
            }

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if ((string?)item.Tag == lang)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            
            DataContext = ViewModel;

            if (Enum.TryParse<ElementTheme>(ViewModel.SelectedTheme, out var theme) && App._mainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme;
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = lang;
                Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
            }
        }

        private void ReplayTutorial_Click(object sender, RoutedEventArgs e)
        {
            App._mainWindow?.NavigateToTutorial();
        }
    }
}
