using System.Runtime.Versioning;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.Database;

namespace NeoCardium.Views
{
    public sealed partial class CategoryDialog : ContentDialog
    {
        public string EnteredCategoryName
        {
            get => CategoryNameTextBox.Text;
            set => CategoryNameTextBox.Text = value;
        }

        public CategoryDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
            {
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true; // Verhindert das Schlieﬂen des Dialogs
            }
        }

        private void ContentDialog_OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide(); // Schlieﬂt den Dialog
        }
    }
}
