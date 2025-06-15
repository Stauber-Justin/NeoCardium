using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Windows.Storage;
using NeoCardium.Services;

namespace NeoCardium.ViewModels
{
    public class SettingsPageViewModel : ObservableObject
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private const string ThemeKey = "AppTheme";
        private const string StatsKey = "ShowStatistics";
        private const string LicenseKey = "LicenseStatus";
        private const string ReminderEnabledKey = "ReminderEnabled";
        private const string ReminderTimeKey = "ReminderTime";

        private string _selectedTheme = "Default";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    localSettings.Values[ThemeKey] = value;
                    App.ApplyTheme(value);
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

        private bool _reminderEnabled;
        public bool ReminderEnabled
        {
            get => _reminderEnabled;
            set
            {
                if (SetProperty(ref _reminderEnabled, value))
                {
                    localSettings.Values[ReminderEnabledKey] = value;
                    if (value)
                    {
                        ReminderService.ScheduleDailyReminder(ReminderTime);
                    }
                    else
                    {
                        ReminderService.CancelReminder();
                    }
                }
            }
        }

        private TimeSpan _reminderTime = new(9, 0, 0);
        public TimeSpan ReminderTime
        {
            get => _reminderTime;
            set
            {
                if (SetProperty(ref _reminderTime, value))
                {
                    localSettings.Values[ReminderTimeKey] = value.ToString();
                    if (ReminderEnabled)
                    {
                        ReminderService.ScheduleDailyReminder(value);
                    }
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

            if (localSettings.Values.TryGetValue(ReminderEnabledKey, out var remEnabledObj) && remEnabledObj is bool storedEnabled)
            {
                _reminderEnabled = storedEnabled;
            }

            if (localSettings.Values.TryGetValue(ReminderTimeKey, out var remTimeObj) && remTimeObj is string storedTime && TimeSpan.TryParse(storedTime, out var span))
            {
                _reminderTime = span;
            }
            App.ApplyTheme(_selectedTheme);
        }
    }
}
