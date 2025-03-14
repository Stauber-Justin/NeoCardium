using System;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NeoCardium.Database;
using NeoCardium.Helpers;

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

        private async void ContentDialog_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
                {
                    ErrorMessageTextBlock.Text = "Kategorie darf nicht leer sein.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true; // Dialog bleibt offen
                    return;
                }

                // Prüfen, ob Kategorie bereits existiert
                if (DatabaseHelper.Instance.CategoryExists(CategoryNameTextBox.Text))
                {
                    ErrorMessageTextBlock.Text = "Diese Kategorie existiert bereits.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
                {
                    ErrorMessageTextBlock.Text = "Kategorie darf nicht leer sein.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true; // Dialog bleibt offen
                    return;
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Unerwarteter Fehler beim Speichern.", ex, this.XamlRoot);
                args.Cancel = true;
            }
        }

        private void ContentDialog_OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide(); // Schließt den Dialog
        }
    }
}
