using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardsPage : Page
    {
        public ObservableCollection<Flashcard> Flashcards { get; private set; } = new ObservableCollection<Flashcard>();
        private int _selectedCategoryId;

        public FlashcardsPage()
        {
            this.InitializeComponent();
            this.DataContext = this; // For XAML binding
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is int categoryId)
            {
                _selectedCategoryId = categoryId;
                _ = LoadFlashcards(categoryId);
            }
        }

        private async Task LoadFlashcards(int categoryId)
        {
            try
            {
                Flashcards.Clear();
                foreach (var flashcard in DatabaseHelper.GetFlashcardsByCategory(categoryId))
                {
                    Flashcards.Add(flashcard);
                }
                FlashcardsListView.ItemsSource = Flashcards;
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Laden der Karteikarten.", ex, this.XamlRoot);
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
                    bool success = DatabaseHelper.AddFlashcard(_selectedCategoryId, dialog.Question, dialog.Answers.ToList());
                    if (!success)
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gespeichert werden.", null, this.XamlRoot);
                        return;
                    }
                    await LoadFlashcards(_selectedCategoryId);
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
                if (sender is not ListView listView || listView.SelectedItem is not Flashcard selectedFlashcard)
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
                    await LoadFlashcards(_selectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Bearbeiten der Karteikarte.", ex, this.XamlRoot);
            }
        }

        // Delete flashcards (handles both single and multi selection)
        private async void DeleteFlashcard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedFlashcards = FlashcardsListView.SelectedItems
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
                    foreach (var flashcard in selectedFlashcards)
                    {
                        DatabaseHelper.DeleteFlashcard(flashcard.Id);
                    }
                    await LoadFlashcards(_selectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarten.", ex, this.XamlRoot);
            }
        }

        // Prevent automatic edit if no modifier is pressed and only one item is selected.
        private void FlashcardsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If multiple items are selected or a modifier key is pressed, do nothing.
            if (FlashcardsListView.SelectedItems.Count > 1 || KeyboardHelper.IsModifierKeyPressed())
            {
                Debug.WriteLine("Multi-select aktiv – Bearbeitung nicht automatisch gestartet.");
                return;
            }

            if (FlashcardsListView.SelectedItem is Flashcard selectedFlashcard)
            {
                Debug.WriteLine($"Bearbeite Flashcard: {selectedFlashcard.Question}");
                // Here you might want to trigger edit only on single-click; if desired uncomment the next line:
                // EditFlashcard_Click(sender, new RoutedEventArgs());
            }
        }

        private void FlashcardsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is Flashcard flashcard)
            {
                // If the item under the pointer is not already selected, clear and select it.
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
