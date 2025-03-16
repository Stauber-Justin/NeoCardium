using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace NeoCardium.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        private bool _isDarkTheme;

        /// <summary>
        /// Gets or sets whether the dark theme is enabled.
        /// Initially set based on the current app theme.
        /// </summary>
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public SettingsPageViewModel()
        {
            // Initialize the theme based on the current app setting.
            // This is a simple check; for a more robust implementation consider reading from persistent storage.
            IsDarkTheme = (Application.Current.RequestedTheme == ApplicationTheme.Dark);
        }
    }
}
