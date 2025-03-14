﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using System.Collections.Generic;

namespace NeoCardium.ViewModels
{
    public class FlashcardsPageViewModel : ObservableObject
    {
        public ObservableCollection<Flashcard> Flashcards { get; } = new ObservableCollection<Flashcard>();

        private int _selectedCategoryId;
        public int SelectedCategoryId
        {
            get => _selectedCategoryId;
            set => SetProperty(ref _selectedCategoryId, value, nameof(SelectedCategoryId));
        }

        public ICommand DeleteFlashcardCommand { get; }
        public ICommand DeleteSelectedFlashcardsCommand { get; }

        public FlashcardsPageViewModel()
        {
            DeleteFlashcardCommand = new RelayCommand(async (param) => await DeleteFlashcardAsync(param as Flashcard));
            DeleteSelectedFlashcardsCommand = new RelayCommand(async (param) => await DeleteSelectedFlashcardsAsync(param as IEnumerable<Flashcard>));
        }

        public async Task LoadFlashcardsAsync(int categoryId)
        {
            SelectedCategoryId = categoryId;
            var flashcards = DatabaseHelper.Instance.GetFlashcardsByCategory(categoryId);
            Flashcards.Clear();
            foreach (var card in flashcards)
            {
                Flashcards.Add(card);
            }
            await Task.CompletedTask;
        }

        private async Task DeleteFlashcardAsync(Flashcard? flashcard)
        {
            if (flashcard == null)
                return;
            DatabaseHelper.Instance.DeleteFlashcard(flashcard.Id);
            await LoadFlashcardsAsync(SelectedCategoryId);
        }

        private async Task DeleteSelectedFlashcardsAsync(IEnumerable<Flashcard>? flashcards)
        {
            if (flashcards == null || !flashcards.Any())
                return;
            foreach (var card in flashcards.ToList())
            {
                DatabaseHelper.Instance.DeleteFlashcard(card.Id);
            }
            await LoadFlashcardsAsync(SelectedCategoryId);
        }
    }
}
