using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.Views
{
    public sealed partial class TutorialPage : Page
    {
        public TutorialPage()
        {
            this.InitializeComponent();
            Loaded += TutorialPage_Loaded;
        }

        private void TutorialPage_Loaded(object sender, RoutedEventArgs e)
        {
            TipCreate.IsOpen = true;
        }

        private void TipCreate_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            TipPractice.IsOpen = true;
        }

        private void TipPractice_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            FinishButton.Visibility = Visibility.Visible;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow main)
            {
                main.NavigateToCategory();
            }
        }
    }
}
