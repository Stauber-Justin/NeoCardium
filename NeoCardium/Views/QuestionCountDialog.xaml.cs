using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.Views
{
    /// <summary>
    /// A ContentDialog that allows the user to select the number of questions.
    /// If "Alle" is selected, SelectedCount is set to -1.
    /// </summary>
    public sealed partial class QuestionCountDialog : ContentDialog
    {
        /// <summary>
        /// The selected number of questions. -1 means "Alle".
        /// </summary>
        public int? SelectedCount { get; private set; } = null;

        public QuestionCountDialog()
        {
            this.InitializeComponent();
        }

        private void FiveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCount = 5;
            this.Hide();
        }

        private void TenButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCount = 10;
            this.Hide();
        }

        private void FifteenButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCount = 15;
            this.Hide();
        }

        private void AllButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCount = -1;
            this.Hide();
        }
    }
}
