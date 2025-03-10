using System;
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
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
        private PracticeMode _selectedMode;
        private PracticeModeOption _selectedModeOption = new();
        private string _feedbackMessage = string.Empty;
        private bool _isFeedbackVisible = false;
        private bool _isSessionActive;
        private bool _isRetryModeEnabled;
        private bool _isFinalStatisticsVisible;

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
                    if(_isRetryModeEnabled == false)
                        _totalCorrect++;
                    if(_incorrectQuestions.Contains(CurrentQuestion))
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
                        _incorrectQuestions.Add(CurrentQuestion);
                    CurrentQuestion.UpdateIncorrectCount();
                    DatabaseHelper.UpdateFlashcardStats(CurrentQuestion.Id, false);
                    FeedbackMessage = $"⚠Falsch⚠\n Die richtige Antwort lautet :  {AnswerOptions.First(a => a.IsCorrect).AnswerText}";
                    IsFeedbackVisible = true;
                    await Task.Delay(2000);
                }

                IsFeedbackVisible = false;
                await LoadNextQuestionAsync();
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

        public PracticeMode SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

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

        public int TotalCorrect
        {
            get => _totalCorrect;
            set => SetProperty(ref _totalCorrect, value);
        }

        public int TotalIncorrect
        {
            get => _totalIncorrect;
            set => SetProperty(ref _totalIncorrect, value);
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

        public bool IsSessionActive
        {
            get => _isSessionActive;
            set
            {
                if (SetProperty(ref _isSessionActive, value))
                {
                    OnPropertyChanged(nameof(IsCategorySelectionVisible));
                }
            }
        }

        public bool IsFinalStatisticsVisible
        {
            get => _isFinalStatisticsVisible;
            set
            {
                if (SetProperty(ref _isFinalStatisticsVisible, value))
                {
                    OnPropertyChanged(nameof(IsCategorySelectionVisible));
                }
            }
        }
        public bool IsCategorySelectionVisible => !IsSessionActive && !IsFinalStatisticsVisible;

        [RelayCommand]
        public void LoadCategories()
        {
            try
            {
                var categories = DatabaseHelper.GetCategories();
                if (categories == null || categories.Count == 0)
                {
                    ExceptionHelper.LogError("Keine Kategorien gefunden!");
                    FeedbackMessage = "⚠ Keine Kategorien verfügbar!";
                    IsFeedbackVisible = true;
                }
                else
                {
                    Categories = new ObservableCollection<Category>(categories);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Laden der Kategorien.", ex);
                FeedbackMessage = "⚠ Fehler beim Laden der Kategorien!";
                IsFeedbackVisible = true;
            }
        }

        [RelayCommand]
        public async Task StartPracticeAsync(Category? selectedCategory)
        {
            try
            {
                if (selectedCategory == null)
                {
                    FeedbackMessage = "⚠ Fehler: Keine Kategorie ausgewählt!";
                    IsFeedbackVisible = true;
                    return;
                }
                _totalCorrect = 0;
                _totalIncorrect = 0;

                Console.WriteLine($"StartPractice: Lade Fragen für Kategorie ID {selectedCategory.Id}");
                var flashcards = DatabaseHelper.GetFlashcardsByCategory(selectedCategory.Id);

                if (flashcards == null || flashcards.Count == 0)
                {
                    ExceptionHelper.LogError($"Keine Karteikarten in Kategorie {selectedCategory.Id} gefunden!");
                    FeedbackMessage = "⚠ Diese Kategorie enthält keine Fragen.";
                    IsFeedbackVisible = true;
                    return;
                }

                Questions = new ObservableCollection<Flashcard>(flashcards);
                _usedQuestions.Clear();

                if (SelectedMode == PracticeMode.MultipleChoice)
                {
                    await LoadNextQuestionAsync();  // ✅ Jetzt korrekt async!
                }
                else if (SelectedMode == PracticeMode.Flashcard)
                {
                    LoadFlashcard();  // Bleibt synchron, weil keine UI-Animation notwendig ist.
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in StartPracticeAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Starten der Übung!";
                IsFeedbackVisible = true;
            }
        }


        [RelayCommand]
        public async Task TogglePracticeSessionAsync(Category? selectedCategory)
        {
            try
            {
                if (!IsSessionActive)
                {
                    if (selectedCategory == null)
                    {
                        FeedbackMessage = "⚠ Fehler: Keine Kategorie ausgewählt!";
                        IsFeedbackVisible = true;
                        return;
                    }

                    await StartPracticeAsync(selectedCategory); // ✅ Jetzt korrekt awaiten!
                }
                else
                {
                    ResetPracticeSession();
                }
                IsSessionActive = !IsSessionActive;
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in TogglePracticeSessionAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Umschalten der Übung!";
                IsFeedbackVisible = true;
            }
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
        public async Task RestartSessionAsync()
        {
            IsFinalStatisticsVisible = false;  // Statistik ausblenden
            IsSessionActive = true;  // Session wieder aktiv setzen

            // UI-Update
            OnPropertyChanged(nameof(IsFinalStatisticsVisible));
            OnPropertyChanged(nameof(IsSessionActive));

            await StartPracticeAsync(SelectedCategory);  // Gleiche Kategorie neu starten
        }

        [RelayCommand]
        public void CloseSession()
        {
            IsFinalStatisticsVisible = false;
            IsSessionActive = false;
            ResetPracticeSession();
        }

        [RelayCommand]
        public async Task LoadNextQuestionAsync()
        {
            try
            {
                if (Questions.Count == 0 || _usedQuestions.Count >= Questions.Count)
                {
                    if (_incorrectQuestions.Count > 0)
                    {
                        _isRetryModeEnabled = true;
                        Questions = new ObservableCollection<Flashcard>(_incorrectQuestions);
                        _usedQuestions.Clear();
                        _incorrectQuestions.Clear();
                        FeedbackMessage = "🔁 Wiederholung der falsch beantworteten Fragen.";
                        IsFeedbackVisible = true;
                    }
                    else
                    {
                        _isRetryModeEnabled = false;
                        await ShowFinalStatisticsAsync();
                        return;
                    }
                }

                Flashcard nextQuestion;
                int attempts = 0;
                do
                {
                    nextQuestion = Questions[_random.Next(Questions.Count)];
                    attempts++;

                    if (attempts > 100) // 🛑 Notausgang für Endlosschleife
                    {
                        ExceptionHelper.LogError("⚠ Endlosschleife in LoadNextQuestionAsync erkannt!");
                        FeedbackMessage = "⚠ Fehler beim Laden der nächsten Frage!";
                        IsFeedbackVisible = true;
                        return;
                    }

                } while (_usedQuestions.Contains(nextQuestion.Id));

                _usedQuestions.Add(nextQuestion.Id);
                CurrentQuestion = nextQuestion;

                LoadAnswers();
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("❌ Fehler in LoadNextQuestionAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Laden der nächsten Frage!";
                IsFeedbackVisible = true;
            }
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
            try
            {
                if (CurrentQuestion == null)
                {
                    ExceptionHelper.LogError("LoadAnswers wurde aufgerufen, aber CurrentQuestion ist null!");
                    FeedbackMessage = "⚠ Fehler: Keine aktuelle Frage gesetzt!";
                    IsFeedbackVisible = true;
                    return;
                }

                var answers = DatabaseHelper.GetRandomAnswersForFlashcard(CurrentQuestion.Id);
                if (answers == null || answers.Count == 0)
                {
                    ExceptionHelper.LogError($"Keine Antworten für Frage {CurrentQuestion.Id} gefunden!");
                    FeedbackMessage = "⚠ Keine Antworten verfügbar!";
                    IsFeedbackVisible = true;
                    AnswerOptions = new ObservableCollection<FlashcardAnswer>(); // Setzt eine leere Liste
                }
                else
                {
                    AnswerOptions = new ObservableCollection<FlashcardAnswer>(answers);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in LoadAnswers().", ex);
                FeedbackMessage = "⚠ Fehler beim Laden der Antworten!";
                IsFeedbackVisible = true;
                AnswerOptions = new ObservableCollection<FlashcardAnswer>(); // Sicherer Fallback
            }
        }

        private async Task ShowFinalStatisticsAsync()
        {
            try
            {
                // Session beenden
                IsSessionActive = false;
                IsFinalStatisticsVisible = true;

                // UI-Update
                OnPropertyChanged(nameof(TotalIncorrect));
                OnPropertyChanged(nameof(TotalCorrect));
                OnPropertyChanged(nameof(IsFinalStatisticsVisible));
                OnPropertyChanged(nameof(IsSessionActive));

                await Task.Delay(1000); // Smooth transition
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Anzeigen der Statistik.", ex);
            }
        }
    }
}