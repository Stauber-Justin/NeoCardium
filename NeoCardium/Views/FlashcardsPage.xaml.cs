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
using NeoCardium.Models;
using NeoCardium.Database;

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
        private void LoadFlashcards(int categoryId)
        {
            Flashcards.Clear();
            foreach (var flashcard in DatabaseHelper.GetFlashcardsByCategory(categoryId))
            {
                Flashcards.Add(flashcard);
            }
            FlashcardsListView.ItemsSource = Flashcards;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is int categoryId)
            {
                _selectedCategoryId = categoryId;
                LoadFlashcards(categoryId);
            }
        }

        private async void AddFlashcard_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FlashcardDialog();
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.Question) && dialog.Answers.Any())
            {
                DatabaseHelper.AddFlashcard(_selectedCategoryId, dialog.Question, dialog.Answers.ToList());
                LoadFlashcards(_selectedCategoryId);
            }
        }

        private async void EditFlashcard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Flashcard selectedFlashcard)
            {
                var dialog = new FlashcardDialog();
                dialog.Question = selectedFlashcard.Question;
                dialog.XamlRoot = this.XamlRoot;

                // Antworten aus DB abrufen und in den Dialog setzen
                dialog.Answers.Clear();
                foreach (var answer in DatabaseHelper.GetAnswersByFlashcard(selectedFlashcard.Id))
                {
                    dialog.Answers.Add(answer);
                }

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.Question) && dialog.Answers.Any())
                {
                    DatabaseHelper.UpdateFlashcard(selectedFlashcard.Id, dialog.Question, dialog.Answers.ToList());
                    LoadFlashcards(_selectedCategoryId);
                }
            }
        }

        private async void DeleteFlashcard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Flashcard selectedFlashcard)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Karteikarte löschen",
                    Content = $"Möchtest du die Karteikarte '{selectedFlashcard.Question}' wirklich löschen?",
                    PrimaryButtonText = "Löschen",
                    CloseButtonText = "Abbrechen",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    DatabaseHelper.DeleteFlashcard(selectedFlashcard.Id);
                    LoadFlashcards(_selectedCategoryId);
                }
            }
        }
    }
}
