﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoCardium.Helpers;
using NeoCardium.Models;
using NeoCardium.Database;
using System.Windows.Input;

namespace NeoCardium.ViewModels
{
    public partial class PracticePageViewModel : ObservableObject
    {
        private readonly Random _random = new Random();
        private HashSet<int> _usedQuestions = new HashSet<int>();
        private List<Flashcard> _incorrectQuestions = new List<Flashcard>();
        private int _totalCorrect = 0;
        private int _totalIncorrect = 0;

        private ObservableCollection<Category> _categories = new();
        private Category? _selectedCategory = null;
        private ObservableCollection<Flashcard> _questions = new();
        private Flashcard _currentQuestion = new Flashcard { Question = "Lade Frage..." };
        private ObservableCollection<FlashcardAnswer> _answerOptions = new();
        public string AnswerButtonText => IsAnswerRevealed ? "Nächste Frage" : "Antwort anzeigen";
        private bool _isAnswerRevealed;
        private string _feedbackMessage = string.Empty;
        private bool _isFeedbackVisible = false;
        private bool _isSessionActive;

        public ICommand CheckAnswerAsync { get; }

        public PracticePageViewModel()
        {
            CheckAnswerAsync = new RelayCommand<FlashcardAnswer>(async (selectedAnswer) =>
            {
                if (selectedAnswer == null)
                {
                    Console.WriteLine("⚠ Fehler: Keine Antwort ausgewählt.");
                    return;
                }

                Console.WriteLine($"Antwort geklickt: {selectedAnswer.AnswerText}");

                if (selectedAnswer.IsCorrect)
                {
                    _totalCorrect++;
                    _incorrectQuestions.Remove(CurrentQuestion);
                    CurrentQuestion.UpdateCorrectCount();
                    DatabaseHelper.UpdateFlashcardStats(CurrentQuestion.Id, true);
                    FeedbackMessage = "✅ Richtig!";
                    IsFeedbackVisible = true;
                    await Task.Delay(750);
                }
                else
                {
                    _totalIncorrect++;
                    if (!_incorrectQuestions.Contains(CurrentQuestion))
                    {
                        _incorrectQuestions.Add(CurrentQuestion);
                    }
                    CurrentQuestion.UpdateIncorrectCount();
                    DatabaseHelper.UpdateFlashcardStats(CurrentQuestion.Id, false);
                    FeedbackMessage = $"⚠Falsch⚠\n Die richtige Antwort lautet :  {AnswerOptions.First(a => a.IsCorrect).AnswerText}";
                    IsFeedbackVisible = true;
                    await Task.Delay(2000);
                }
                
                IsFeedbackVisible = false;
                LoadNextQuestion();
            });
            _random = new Random((int)DateTime.Now.Ticks);
            _usedQuestions = new HashSet<int>();
            Categories = new ObservableCollection<Category>();
            Questions = new ObservableCollection<Flashcard>();
            AnswerOptions = new ObservableCollection<FlashcardAnswer>();
            CurrentQuestion = new Flashcard { Question = "Lade Frage..." };
            FeedbackMessage = string.Empty;
            IsFeedbackVisible = false;

            LoadCategories();
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ObservableCollection<Flashcard> Questions
        {
            get => _questions;
            set => SetProperty(ref _questions, value);
        }

        public Flashcard CurrentQuestion
        {
            get => _currentQuestion;
            set => SetProperty(ref _currentQuestion, value);
        }

        public ObservableCollection<FlashcardAnswer> AnswerOptions
        {
            get => _answerOptions;
            set => SetProperty(ref _answerOptions, value);
        }

        public bool IsAnswerRevealed
        {
            get => _isAnswerRevealed;
            set
            {
                SetProperty(ref _isAnswerRevealed, value);
                OnPropertyChanged(nameof(IsAnswerRevealed));
                OnPropertyChanged(nameof(AnswerButtonText)); // Erzwingt UI-Update!
            }
        }

        public ObservableCollection<PracticeModeOption> AvailableModes { get; } = new ObservableCollection<PracticeModeOption>
        {
            new PracticeModeOption { Mode = PracticeMode.MultipleChoice, ModeName = "Multiple-Choice" },
            new PracticeModeOption { Mode = PracticeMode.Flashcard, ModeName = "Karteikarten-Modus" }
        };

        private PracticeMode _selectedMode;
        public PracticeMode SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        private PracticeModeOption _selectedModeOption;
        public PracticeModeOption SelectedModeOption
        {
            get => _selectedModeOption;
            set
            {
                if (SetProperty(ref _selectedModeOption, value) && value != null)
                {
                    SelectedMode = value.Mode;
                }
            }
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public bool IsFeedbackVisible
        {
            get => _isFeedbackVisible;
            set => SetProperty(ref _isFeedbackVisible, value);
        }

        [RelayCommand]
        public void LoadCategories()
        {
            Categories = new ObservableCollection<Category>(DatabaseHelper.GetCategories());
        }

        public bool IsSessionActive
        {
            get => _isSessionActive;
            set => SetProperty(ref _isSessionActive, value);
        }


        [RelayCommand]
        public void StartPractice(Category? selectedCategory)
        {
            if (selectedCategory == null)
            {
                FeedbackMessage = "⚠ Fehler: Keine Kategorie ausgewählt!";
                IsFeedbackVisible = true;
                return;
            }

            Console.WriteLine($"StartPractice: Lade Fragen für Kategorie ID {selectedCategory.Id}");
            var flashcards = DatabaseHelper.GetFlashcardsByCategory(selectedCategory.Id);

            if (flashcards == null || flashcards.Count == 0)
            {
                FeedbackMessage = "⚠ Diese Kategorie enthält keine Fragen.";
                IsFeedbackVisible = true;
                Console.WriteLine("Keine Karteikarten gefunden!");
                return;
            }

            Console.WriteLine($"{flashcards.Count} Fragen geladen.");
            Questions = new ObservableCollection<Flashcard>(flashcards);
            _usedQuestions.Clear();

            if (SelectedMode == PracticeMode.MultipleChoice)
            {
                LoadNextQuestion();  // Starte Multiple-Choice-Modus
            }
            else if (SelectedMode == PracticeMode.Flashcard)
            {
                LoadFlashcard();  // Methode für Flashcard-Modus (kommt später)
            }
        }

        [RelayCommand]
        public void TogglePracticeSession(Category selectedCategory)
        {
            if (!IsSessionActive)
            {
                if (selectedCategory == null)
                {
                    FeedbackMessage = "⚠ Fehler: Keine Kategorie ausgewählt!";
                    IsFeedbackVisible = true;
                    return;
                }

                StartPractice(selectedCategory);
            }
            else
            {
                ResetPracticeSession();
            }
            IsSessionActive = !IsSessionActive;
        }


        private void ResetPracticeSession()
        {
            SelectedCategory = null;
            Questions.Clear();
            CurrentQuestion = new Flashcard { Question = "Lade Frage..." };
            AnswerOptions.Clear();
            FeedbackMessage = "";
            IsFeedbackVisible = false;
        }

        [RelayCommand]
        public void LoadNextQuestion()
        {
            if (Questions.Count == 0 || _usedQuestions.Count == Questions.Count)
            {
                if (_incorrectQuestions.Count > 0)
                {
                    Questions = new ObservableCollection<Flashcard>(_incorrectQuestions);
                    _usedQuestions.Clear();
                    _incorrectQuestions.Clear();
                    FeedbackMessage = "🔁 Wiederholung der falsch beantworteten Fragen.";
                    IsFeedbackVisible = true;
                }
                else
                {
                    ShowFinalStatistics();
                    return;
                }
            }

            Console.WriteLine($"Nächste Frage: {CurrentQuestion.Question} (ID: {CurrentQuestion.Id})");
            Flashcard nextQuestion;
            do
            {
                nextQuestion = Questions[_random.Next(Questions.Count)];
            } while (_usedQuestions.Contains(nextQuestion.Id));

            _usedQuestions.Add(nextQuestion.Id);
            CurrentQuestion = new Flashcard
            {
                Id = nextQuestion.Id,
                Question = nextQuestion.Question,
                Answer = nextQuestion.Answer,
                CorrectCount = nextQuestion.CorrectCount,
                IncorrectCount = nextQuestion.IncorrectCount
            };
            LoadAnswers();
        }
        public void LoadFlashcard()
        {
            if (Questions.Count == 0)
            {
                FeedbackMessage = "⚠ Keine Fragen in dieser Kategorie!";
                IsFeedbackVisible = true;
                return;
            }

            Flashcard nextFlashcard;
            do
            {
                nextFlashcard = Questions[_random.Next(Questions.Count)];
            } while (_usedQuestions.Contains(nextFlashcard.Id) && _usedQuestions.Count < Questions.Count);

            _usedQuestions.Add(nextFlashcard.Id);

            // 🛠️ Antwort wird jetzt aus der neuen Funktion geholt
            string correctAnswer = DatabaseHelper.GetCorrectAnswerForFlashcard(nextFlashcard.Id);

            CurrentQuestion = new Flashcard
            {
                Id = nextFlashcard.Id,
                Question = nextFlashcard.Question,
                Answer = correctAnswer, // ✅ Antwort aus der neuen Methode holen!
                CorrectCount = nextFlashcard.CorrectCount,
                IncorrectCount = nextFlashcard.IncorrectCount
            };

            Console.WriteLine($"✅ LoadFlashcard: Frage geladen: {CurrentQuestion.Question}");
            Console.WriteLine($"✅ LoadFlashcard: Antwort geladen: {CurrentQuestion.Answer}");

            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(CurrentQuestion.Answer));

            IsAnswerRevealed = false;
        }

        [RelayCommand]
        public void RevealAnswer()
        {
            if (!IsAnswerRevealed)
            {
                IsAnswerRevealed = true;
            }
            else
            {
                LoadFlashcard();  // Sofort eine neue Frage laden
            }
            OnPropertyChanged(nameof(AnswerButtonText)); // Aktualisiert den Button-Text
        }

        private void LoadAnswers()
        {
            if (CurrentQuestion == null)
            {
                Console.WriteLine("⚠ Fehler: Keine aktuelle Frage gesetzt.");
                return;
            }

            Console.WriteLine($"Lade Antworten für Frage ID: {CurrentQuestion.Id}");
            var answers = DatabaseHelper.GetRandomAnswersForFlashcard(CurrentQuestion.Id);

            if (answers == null)
            {
                Console.WriteLine("⚠ Fehler: Nicht genügend Antworten verfügbar!");
                return;
            }

            AnswerOptions = new ObservableCollection<FlashcardAnswer>(answers);
            while (answers.Count < 4)
            {
                answers.Add(new FlashcardAnswer { AnswerText = "N/A", IsCorrect = false });
            }
        }


        private void ShowFinalStatistics()
        {
            FeedbackMessage = $"📊 Sitzung beendet! Richtig: {_totalCorrect}, Falsch: {_totalIncorrect}";
            IsFeedbackVisible = true;
        }
    }
}