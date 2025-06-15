using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace NeoCardium.ViewModels
{
    public class SettingsPageViewModel : ObservableObject
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private const string ThemeKey = "AppTheme";
        private const string StatsKey = "ShowStatistics";
        private const string LicenseKey = "LicenseStatus";

        private string _selectedTheme = "Default";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    localSettings.Values[ThemeKey] = value;
                    if (Enum.TryParse<ElementTheme>(value, out var theme) && App._mainWindow?.Content is FrameworkElement root)
                    {
                        root.RequestedTheme = theme;
                    }
                }
            }
        }

        private bool _showStatistics;
        public bool ShowStatistics
        {
            get => _showStatistics;
            set
            {
                if (SetProperty(ref _showStatistics, value))
                {
                    localSettings.Values[StatsKey] = value;
                }
            }
        }

        private string _licenseStatus = "Free";
        public string LicenseStatus
        {
            get => _licenseStatus;
            set
            {
                if (SetProperty(ref _licenseStatus, value))
                {
                    localSettings.Values[LicenseKey] = value;
                }
            }
        }

        public SettingsPageViewModel()
        {
            if (localSettings.Values.TryGetValue(ThemeKey, out var themeObj) && themeObj is string storedTheme)
            {
                _selectedTheme = storedTheme;
            }

            if (localSettings.Values.TryGetValue(StatsKey, out var statsObj) && statsObj is bool storedStats)
            {
                _showStatistics = storedStats;
            }

            if (localSettings.Values.TryGetValue(LicenseKey, out var licObj) && licObj is string storedLicense)
            {
                _licenseStatus = storedLicense;
            }
        }
    }
}
