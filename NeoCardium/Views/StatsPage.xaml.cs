using Microsoft.UI.Xaml.Controls;
using NeoCardium.ViewModels;

namespace NeoCardium.Views
{
    public sealed partial class StatsPage : Page
    {
        public StatsPageViewModel ViewModel { get; } = new StatsPageViewModel();

        public StatsPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
