using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.ViewModels;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardsPage : Page
    {
        // Expose the ViewModel for x:Bind usage.
        public FlashcardsPageViewModel ViewModel { get; } = new FlashcardsPageViewModel();

        // Flag to suppress auto-edit when a right-tap occurs.
        private bool _suppressAutoEdit = false;

        public FlashcardsPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is int categoryId)
            {
                await ViewModel.LoadFlashcardsAsync(categoryId);
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            } else
            {
                Frame.Navigate(typeof(CategoryPage));
            }
        }

        private async void AddFlashcard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new FlashcardDialog { XamlRoot = this.XamlRoot };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary &&
                    !string.IsNullOrWhiteSpace(dialog.Question) &&
                    dialog.Answers.Any())
                {
                    // Use the singleton instance for DatabaseHelper
                    bool success = DatabaseHelper.Instance.AddFlashcard(ViewModel.SelectedCategoryId, dialog.Question, dialog.Answers.ToList(), out string errorMsg);
                    if (!success)
                    {
                        if (errorMsg == "duplicate")
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Es existiert bereits eine Karteikarte mit diesem Namen, bitte verwende einen anderen!", null, this.XamlRoot);
                            // Do not close the dialog: let the user correct the question.
                            return;
                        }
                        else
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gespeichert werden.", null, this.XamlRoot);
                            return;
                        }
                    }
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Erstellen einer Karteikarte.", ex, this.XamlRoot);
            }
        }

        private async void BatchCreateButton_Click(object sender, RoutedEventArgs e)
        {
            // new BATCH-create logic
            var batchDialog = new FlashcardDialogBatch
            {
                XamlRoot = this.XamlRoot,
                CategoryId = ViewModel.SelectedCategoryId
            };

            await batchDialog.ShowAsync();
            // after it closes, we can refresh the list:
            await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
        }

        private async void EditFlashcard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FlashcardsListView.SelectedItem is not Flashcard selectedFlashcard)
                {
                    Debug.WriteLine("FEHLER: EditFlashcard_Click - Kein gültiges Flashcard ausgewählt.");
                    return;
                }

                var dialog = new FlashcardDialog
                {
                    Question = selectedFlashcard.Question,
                    XamlRoot = this.XamlRoot
                };

                dialog.Answers.Clear();
                foreach (var answer in DatabaseHelper.Instance.GetAnswersByFlashcard(selectedFlashcard.Id))
                {
                    dialog.Answers.Add(answer);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary &&
                    !string.IsNullOrWhiteSpace(dialog.Question) &&
                    dialog.Answers.Any())
                {
                    bool updateSuccess = DatabaseHelper.Instance.UpdateFlashcard(selectedFlashcard.Id, dialog.Question, dialog.Answers.ToList(), out string errorMsg);
                    if (!updateSuccess)
                    {
                        if (errorMsg == "duplicate")
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Es existiert bereits eine Karteikarte mit diesem Namen, bitte verwende einen anderen!", null, this.XamlRoot);
                            return;
                        }
                        else
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gespeichert werden.", null, this.XamlRoot);
                            return;
                        }
                    }
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Bearbeiten der Karteikarte.", ex, this.XamlRoot);
            }
        }

        private async void DeleteFlashcard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedFlashcards = FlashcardsListView.SelectedItems
                    .Cast<object>()
                    .Select(item => item as Flashcard)
                    .Where(card => card != null)
                    .Cast<Flashcard>()
                    .ToList();

                if (!selectedFlashcards.Any())
                {
                    Debug.WriteLine("Kein Flashcard ausgewählt.");
                    return;
                }

                string message = selectedFlashcards.Count == 1
                    ? $"Möchtest du die Karteikarte '{selectedFlashcards[0].Question}' wirklich löschen?"
                    : $"Möchtest du die {selectedFlashcards.Count} ausgewählten Karteikarten wirklich löschen?";

                var confirmDialog = new ContentDialog
                {
                    Title = "Karteikarten löschen",
                    Content = message,
                    PrimaryButtonText = "Löschen",
                    CloseButtonText = "Abbrechen",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    foreach (var flashcard in selectedFlashcards)
                    {
                        DatabaseHelper.Instance.DeleteFlashcard(flashcard.Id);
                    }
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarten.", ex, this.XamlRoot);
            }
        }

        // When selection changes due to a left-click (without modifiers) auto-edit is triggered.
        // We check if _suppressAutoEdit is set (by a right-click) and do nothing if so.
        private void FlashcardsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlashcardsListView == null || FlashcardsListView.SelectedItem == null)
            {
                Debug.WriteLine("Keine Flashcard ausgewählt.");
                return;
            }

            if (_suppressAutoEdit)
            {
                // Reset the flag and do not auto-edit.
                _suppressAutoEdit = false;
                return;
            }

            if (FlashcardsListView.SelectedItems.Count > 1 || KeyboardHelper.IsModifierKeyPressed())
            {
                Debug.WriteLine("Multi-select aktiv – Bearbeitung nicht automatisch gestartet.");
                return;
            }

            if (FlashcardsListView.SelectedItem is Flashcard selectedFlashcard)
            {
                Debug.WriteLine($"Bearbeite Flashcard: {selectedFlashcard.Question}");
                // Trigger the edit behavior on single left-click.
                EditFlashcard_Click(sender, new RoutedEventArgs());
            }
        }

        // Right-tap opens the context menu without triggering auto-edit.
        private void FlashcardsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Set flag to suppress auto-edit on selection change.
            _suppressAutoEdit = true;
            if (e.OriginalSource is FrameworkElement element && element.DataContext is Flashcard flashcard)
            {
                // If the flashcard under the pointer is not already selected, clear selection and select it.
                if (!FlashcardsListView.SelectedItems.Cast<Flashcard>().Any(f => f.Id == flashcard.Id))
                {
                    FlashcardsListView.SelectedItems.Clear();
                    FlashcardsListView.SelectedItems.Add(flashcard);
                }

                int selectedCount = FlashcardsListView.SelectedItems.Count;
                MenuFlyout flyout = new MenuFlyout();
                MenuFlyoutItem deleteItem = new MenuFlyoutItem
                {
                    Text = selectedCount > 1 ? "Ausgewählte löschen" : "Löschen"
                };
                deleteItem.Click += DeleteFlashcard_Click;
                flyout.Items.Add(deleteItem);
                flyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            }
            else
            {
                // If right-tapped on an area with no flashcard, clear selection.
                FlashcardsListView.SelectedItems.Clear();
            }
        }
    }
}
