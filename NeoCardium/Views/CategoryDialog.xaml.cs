using System;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NeoCardium.Database;
using NeoCardium.Helpers;

using Windows.ApplicationModel.Resources;
namespace NeoCardium.Views
{
    public sealed partial class CategoryDialog : ContentDialog
    {
        private readonly ResourceLoader _loader = new("Resources");
        public string EnteredCategoryName
        {
            get => CategoryNameTextBox.Text;
            set => CategoryNameTextBox.Text = value;
        }

        public CategoryDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

                string name = CategoryNameTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ErrorMessageTextBlock.Text = _loader.GetString("CategoryDialog_ErrorEmpty.Text");
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                if (DatabaseHelper.Instance.CategoryExists(name))
                {
                    ErrorMessageTextBlock.Text = _loader.GetString("CategoryDialog_ErrorExists.Text");
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                EnteredCategoryName = name;
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync(_loader.GetString("CategoryDialog_SaveError.Text"), ex, this.XamlRoot);
                args.Cancel = true;
            }
        }

        private void ContentDialog_OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide(); // Schließt den Dialog
        }
    }
}
