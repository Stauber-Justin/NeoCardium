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
            await ShowFlashcardDialogAsync(isEditMode: false, flashcard: null);
        }

        /// <summary>
        /// Shows the FlashcardDialog for creating or editing a flashcard.
        /// If the user enters a duplicate flashcard name, an error message is shown and the dialog is re-shown with the previous content intact.
        /// For other errors, a generic error is shown and the dialog is closed.
        /// </summary>
        private async Task ShowFlashcardDialogAsync(bool isEditMode, Flashcard? flashcard)
        {
            // Create an instance of the dialog
            var dialog = new FlashcardDialog { XamlRoot = this.XamlRoot };

            if (isEditMode && flashcard != null)
            {
                dialog.Question = flashcard.Question;
                dialog.Answers.Clear();
                foreach (var answer in DatabaseHelper.GetAnswersByFlashcard(flashcard.Id))
                {
                    dialog.Answers.Add(answer);
                }
            }

            while (true)
            {
                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                    return;

                if (string.IsNullOrWhiteSpace(dialog.Question) || !dialog.Answers.Any())
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Frage und mindestens eine Antwort sind erforderlich.", null, this.XamlRoot);
                    continue;
                }

                string errorMessage;
                bool success;

                if (isEditMode && flashcard != null)
                {
                    success = DatabaseHelper.UpdateFlashcard(flashcard.Id, dialog.Question, dialog.Answers.ToList(), out errorMessage);
                }
                else
                {
                    success = DatabaseHelper.AddFlashcard(ViewModel.SelectedCategoryId, dialog.Question, dialog.Answers.ToList(), out errorMessage);
                }

                if (success)
                {
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                    return;
                }
                else
                {
                    if (errorMessage == "duplicate")
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Es existiert bereits eine Karteikarte mit diesem Namen, bitte verwende einen anderen!", null, this.XamlRoot);
                        // Loop again so the dialog stays open for user to modify the name.
                        continue;
                    }
                    else
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gespeichert werden.", null, this.XamlRoot);
                        return;
                    }
                }
            }
        }

        private async void EditFlashcard_Click(object sender, RoutedEventArgs e)
        {
            if (FlashcardsListView.SelectedItem is Flashcard selectedFlashcard)
            {
                await ShowFlashcardDialogAsync(isEditMode: true, flashcard: selectedFlashcard);
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
                    await ViewModel.LoadFlashcardsAsync(ViewModel.SelectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarten.", ex, this.XamlRoot);
            }
        }

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
                EditFlashcard_Click(sender, new RoutedEventArgs());
            }
        }

        private void FlashcardsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is Flashcard flashcard)
            {
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
