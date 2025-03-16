using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.ViewModels;
using System.Diagnostics;

namespace NeoCardium.Views
{
    public sealed partial class PracticePage : Page
    {
        public PracticePageViewModel ViewModel { get; }

        public PracticePage()
        {
            this.InitializeComponent(); // Make sure your XAML Build Action is set to "Page"
            ViewModel = new PracticePageViewModel();
            this.DataContext = ViewModel;
            this.Loaded += PracticePage_Loaded;
            Debug.WriteLine($"[PracticePage] Constructor => IsFinalStatisticsVisible={ViewModel.IsFinalStatisticsVisible}, IsSessionActive={ViewModel.IsSessionActive}");
        }

        private async void PracticePage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PracticePage] Loaded -> calling ViewModel.LoadCategoriesAsync()");
            await ViewModel.LoadCategoriesAsync();
        }
    }
}
