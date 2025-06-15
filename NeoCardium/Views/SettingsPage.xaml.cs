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
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NeoCardium.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
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
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = lang;
                Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
            }
        }
    }
}
