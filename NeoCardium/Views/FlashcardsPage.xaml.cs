using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.ViewModels;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardsPage : Page
    {
        // Expose the new ViewModel as a public property for x:Bind.
        public FlashcardsPageViewModel ViewModel { get; } = new FlashcardsPageViewModel();

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
                    bool success = DatabaseHelper.AddFlashcard(ViewModel.SelectedCategoryId, dialog.Question, dialog.Answers.ToList());
                    if (!success)
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gespeichert werden.", null, this.XamlRoot);
                        return;
                    }
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Erstellen einer Karteikarte.", ex, this.XamlRoot);
            }
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
                foreach (var answer in DatabaseHelper.GetAnswersByFlashcard(selectedFlashcard.Id))
                {
                    dialog.Answers.Add(answer);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary &&
                    !string.IsNullOrWhiteSpace(dialog.Question) &&
                    dialog.Answers.Any())
                {
                    DatabaseHelper.UpdateFlashcard(selectedFlashcard.Id, dialog.Question, dialog.Answers.ToList());
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Bearbeiten der Karteikarte.", ex, this.XamlRoot);
            }
        }

        // Delete flashcards (single or multi) using the shared confirmation dialog helper.
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

                var result = await ConfirmationDialogHelper.ShowDeleteConfirmationDialogAsync(
                    selectedFlashcards,
                    "die Karteikarte",
                    "Karteikarten",
                    f => f.Question,
                    this.XamlRoot);

                if (result == ContentDialogResult.Primary)
                {
                    if (selectedFlashcards.Count > 1)
                    {
                        foreach (var flashcard in selectedFlashcards)
                        {
                            DatabaseHelper.DeleteFlashcard(flashcard.Id);
                        }
                    }
                    else
                    {
                        DatabaseHelper.DeleteFlashcard(selectedFlashcards.First().Id);
                    }
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarten.", ex, this.XamlRoot);
            }
        }

        // When selection changes: if only one item is selected and no modifier is pressed, trigger edit.
        private void FlashcardsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlashcardsListView == null || FlashcardsListView.SelectedItem == null)
            {
                Debug.WriteLine("Keine Flashcard ausgewählt.");
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
                // Automatically trigger the edit behavior on single selection.
                EditFlashcard_Click(sender, new RoutedEventArgs());
            }
        }

        private void FlashcardsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is Flashcard flashcard)
            {
                // If the item under the pointer is not already selected, clear selection and select it.
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
        }
    }
}
