using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NeoCardium.Views
{
    public sealed partial class FlashcardsPage : Page
    {
        public ObservableCollection<Flashcard> Flashcards { get; private set; } = new();

        private int _selectedCategoryId;

        public FlashcardsPage()
        {
            this.InitializeComponent();
            this.DataContext = this; // Setzt das Binding für die XAML-Datei
        }

        // Wird aufgerufen, wenn eine Kategorie ausgewählt wird
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is int categoryId)
            {
                _selectedCategoryId = categoryId;
                _=LoadFlashcards(categoryId); // `_ =` um Async-Warning zu vermeiden
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
                var dialog = new FlashcardDialog();
                dialog.XamlRoot = this.XamlRoot;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.Question) && dialog.Answers.Any())
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

                // Antworten abrufen
                dialog.Answers.Clear();
                foreach (var answer in DatabaseHelper.GetAnswersByFlashcard(selectedFlashcard.Id))
                {
                    dialog.Answers.Add(answer);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.Question) && dialog.Answers.Any())
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

        private async void DeleteFlashcard_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("DeleteFlashcard_Click");
            try
            {
                var menuItem = (MenuFlyoutItem)sender;
                var flashcard = (Flashcard)menuItem.CommandParameter;
                if (flashcard == null) return;

                Debug.WriteLine($"Lösche Flashcard mit ID: {flashcard.Id}");

                var confirmDialog = new ContentDialog
                {
                    Title = "Karteikarte löschen",
                    Content = $"Möchtest du die Karteikarte '{flashcard.Question}' wirklich löschen?",
                    PrimaryButtonText = "Löschen",
                    CloseButtonText = "Abbrechen",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    bool success = DatabaseHelper.DeleteFlashcard(flashcard.Id);
                    if (!success)
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Karteikarte konnte nicht gelöscht werden.", null, this.XamlRoot);
                        return;
                    }

                    FlashcardsListView.ItemsSource = null; // Reset ItemsSource
                    await LoadFlashcards(_selectedCategoryId);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Karteikarte.", ex, this.XamlRoot);
            }
        }

        private void FlashcardsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlashcardsListView == null)
            {
                Debug.WriteLine("FlashcardsListView ist NULL!");
                return;
            }

            if (FlashcardsListView.SelectedItem is Flashcard selectedFlashcard)
            {
                Debug.WriteLine($"Ausgewählte Karteikarte: {selectedFlashcard.Question}");
                EditFlashcard_Click(sender, new RoutedEventArgs());
            }
            else
            {
                Debug.WriteLine("Kein Flashcard ausgewählt oder null!");
            }
        }

        private void FlashcardsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is Flashcard flashcard)
            {
                // Erstellt ein neues Flyout
                MenuFlyout flyout = new MenuFlyout();

                // Erstellt den Lösch-Button
                MenuFlyoutItem deleteItem = new MenuFlyoutItem
                {
                    Text = "Löschen",
                    CommandParameter = flashcard
                };
                deleteItem.Click += DeleteFlashcard_Click;

                flyout.Items.Add(deleteItem);
                flyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            }
        }

    }
}
