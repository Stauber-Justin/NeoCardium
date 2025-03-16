using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.Models;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.ViewModels
{
    /// <summary>
    /// The PracticePageViewModel manages state and logic for practicing flashcards.
    /// It supports two modes:
    ///   - MultipleChoice: (e.g., "Wer-Wird-Millionär" style) with an answer grid.
    ///   - Flashcard mode: a classic flip-style presentation.
    ///
    /// Session Flow:
    ///   1. The user selects a category and practice mode.
    ///   2. TogglePracticeSessionAsync starts or stops the session.
    ///   3. StartPracticeAsync loads flashcards, resets counters, and calls LoadNextQuestionAsync (for MultipleChoice)
    ///      or LoadFlashcard (for Flashcard).
    ///   4. Answer submissions update statistics and load the next question.
    ///   5. When all questions are exhausted, final statistics are displayed.
    /// </summary>
    public partial class PracticePageViewModel : ObservableObject
    {
        #region Fields

        private readonly Random _random;
        private HashSet<int> _usedQuestions;
        private List<Flashcard> _incorrectQuestions;
        private int _totalCorrect;
        private int _totalIncorrect;
        private ObservableCollection<Category> _categories;
        private Category? _selectedCategory;
        private ObservableCollection<Flashcard> _questions;
        private Flashcard _currentQuestion;
        private ObservableCollection<FlashcardAnswer> _answerOptions;
        private bool _isAnswerRevealed;
        private PracticeMode _selectedMode;
        private PracticeModeOption _selectedModeOption;
        private string _feedbackMessage;
        private bool _isFeedbackVisible;
        private bool _isSessionActive;
        private bool _isRetryModeEnabled;
        private bool _isFinalStatisticsVisible;
        private bool _isProcessingAnswer;

        #endregion

        #region Properties

        public bool IsProcessingAnswer
        {
            get => _isProcessingAnswer;
            set
            {
                if (SetProperty(ref _isProcessingAnswer, value))
                    OnPropertyChanged(nameof(IsNotProcessing));
            }
        }
        public bool IsNotProcessing => !IsProcessingAnswer;

        public ICommand CheckAnswerAsync { get; }

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
                OnPropertyChanged(nameof(AnswerButtonText));
            }
        }

        public string AnswerButtonText => IsAnswerRevealed ? "Nächste Frage" : "Antwort anzeigen";

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
                    SelectedMode = value.Mode;
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

        public int TotalAnswered => TotalCorrect + TotalIncorrect;

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
                    OnPropertyChanged(nameof(IsCategorySelectionVisible));
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
                    OnPropertyChanged(nameof(IsStartCancelButtonVisible));
                }
            }
        }

        public bool IsCategorySelectionVisible => !IsSessionActive && !IsFinalStatisticsVisible;
        public bool IsStartCancelButtonVisible => !IsFinalStatisticsVisible;

        #endregion

        #region Constructor

        public PracticePageViewModel()
        {
            _random = new Random((int)DateTime.Now.Ticks);
            _usedQuestions = new HashSet<int>();
            _incorrectQuestions = new List<Flashcard>();
            _totalCorrect = 0;
            _totalIncorrect = 0;
            _categories = new ObservableCollection<Category>();
            _selectedCategory = null;
            _questions = new ObservableCollection<Flashcard>();
            _currentQuestion = new Flashcard { Question = "Lade Frage..." };
            _answerOptions = new ObservableCollection<FlashcardAnswer>();
            _isAnswerRevealed = false;
            _selectedMode = PracticeMode.MultipleChoice;
            _selectedModeOption = AvailableModes.FirstOrDefault(m => m.Mode == PracticeMode.MultipleChoice)
                                  ?? new PracticeModeOption { Mode = PracticeMode.MultipleChoice, ModeName = "Multiple-Choice" };
            _feedbackMessage = string.Empty;
            _isFeedbackVisible = false;
            _isSessionActive = false;
            _isRetryModeEnabled = false;
            _isFinalStatisticsVisible = false;
            IsProcessingAnswer = false;

            CheckAnswerAsync = new RelayCommand<FlashcardAnswer>(async (selectedAnswer) =>
            {
                if (IsProcessingAnswer) return;
                IsProcessingAnswer = true;

                if (selectedAnswer == null)
                {
                    Debug.WriteLine("⚠ Keine Antwort ausgewählt.");
                    IsProcessingAnswer = false;
                    return;
                }

                Debug.WriteLine($"[CheckAnswerAsync] Antwort geklickt: {selectedAnswer.AnswerText}");
                if (selectedAnswer.IsCorrect)
                {
                    if (!_isRetryModeEnabled)
                        _totalCorrect++;
                    if (_incorrectQuestions.Contains(CurrentQuestion))
                        _incorrectQuestions.Remove(CurrentQuestion);

                    CurrentQuestion.UpdateCorrectCount();
                    DatabaseHelper.Instance.UpdateFlashcardStats(CurrentQuestion.Id, true);
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
                    DatabaseHelper.Instance.UpdateFlashcardStats(CurrentQuestion.Id, false);
                    var correctAnswer = AnswerOptions.FirstOrDefault(a => a.IsCorrect)?.AnswerText ?? "Keine korrekte Antwort gefunden";
                    FeedbackMessage = $"⚠ Falsch ⚠\nRichtige Antwort: {correctAnswer}";
                    IsFeedbackVisible = true;
                    await Task.Delay(2000);
                }

                IsFeedbackVisible = false;
                await LoadNextQuestionAsync();
                IsProcessingAnswer = false;
            });

            Debug.WriteLine($"[PracticePageVM] Constructor => IsFinalStatisticsVisible={_isFinalStatisticsVisible}, IsSessionActive={_isSessionActive}");
        }

        #endregion

        #region Public Methods

        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = DatabaseHelper.Instance.GetCategories();
                if (categories == null || categories.Count == 0)
                {
                    await ExceptionHelper.LogErrorAsync("Keine Kategorien gefunden!");
                    FeedbackMessage = "⚠ Keine Kategorien verfügbar!";
                    IsFeedbackVisible = true;
                }
                else
                {
                    Categories = new ObservableCollection<Category>(categories);
                    SelectedCategory = Categories.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.LogErrorAsync("Fehler beim Laden der Kategorien.", ex);
                FeedbackMessage = "⚠ Fehler beim Laden der Kategorien!";
                IsFeedbackVisible = true;
            }
        }

        [RelayCommand]
        public async Task TogglePracticeSessionAsync()
        {
            try
            {
                if (!IsSessionActive)
                {
                    await StartPracticeAsync();
                }
                else
                {
                    ResetPracticeSession();
                    IsSessionActive = false;
                    Debug.WriteLine("[TogglePracticeSessionAsync] => Session stopped");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in TogglePracticeSessionAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Umschalten der Übung!";
                IsFeedbackVisible = true;
            }
        }

        public async Task StartPracticeAsync()
        {
            try
            {
                if (SelectedCategory == null)
                {
                    FeedbackMessage = "⚠ Fehler: Keine Kategorie ausgewählt!";
                    IsFeedbackVisible = true;
                    return;
                }

                Debug.WriteLine($"[StartPracticeAsync] Using category ID={SelectedCategory.Id}, Name='{SelectedCategory.CategoryName}'");

                int desiredCount = -1;
                var allFlashcards = DatabaseHelper.Instance.GetFlashcardsByCategory(SelectedCategory.Id).ToList();
                Debug.WriteLine($"[StartPracticeAsync] Fetched {allFlashcards.Count} flashcards for this category.");

                if (allFlashcards.Count == 0)
                {
                    FeedbackMessage = $"⚠ Keine Karteikarten in '{SelectedCategory.CategoryName}'.";
                    IsFeedbackVisible = true;
                    return;
                }

                List<Flashcard> selectedQuestions = SelectQuestionsForSession(allFlashcards, desiredCount, SelectedMode);
                Debug.WriteLine($"[StartPracticeAsync] => Selected {selectedQuestions.Count} questions for the session.");

                _totalCorrect = 0;
                _totalIncorrect = 0;
                _incorrectQuestions.Clear();
                _isRetryModeEnabled = false;
                _usedQuestions.Clear();

                Questions = new ObservableCollection<Flashcard>(selectedQuestions);
                Debug.WriteLine($"[StartPracticeAsync] => Questions.Count={Questions.Count}");

                if (SelectedMode == PracticeMode.MultipleChoice)
                {
                    Debug.WriteLine("[StartPracticeAsync] => Mode=MultipleChoice, calling LoadNextQuestionAsync()");
                    await LoadNextQuestionAsync();
                }
                else if (SelectedMode == PracticeMode.Flashcard)
                {
                    Debug.WriteLine("[StartPracticeAsync] => Mode=Flashcard, calling LoadFlashcard()");
                    LoadFlashcard();
                }

                IsSessionActive = true;
                IsFinalStatisticsVisible = false;
                OnPropertyChanged(nameof(IsSessionActive));
                OnPropertyChanged(nameof(IsFinalStatisticsVisible));
                Debug.WriteLine($"[StartPracticeAsync] => IsSessionActive={IsSessionActive}, IsFinalStatisticsVisible={IsFinalStatisticsVisible}");
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler in StartPracticeAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Starten der Übung!";
                IsFeedbackVisible = true;
            }
        }

        private List<Flashcard> SelectQuestionsForSession(List<Flashcard> flashcards, int desiredCount, PracticeMode mode)
        {
            List<Flashcard> selected;
            if (mode == PracticeMode.MultipleChoice)
            {
                selected = flashcards.OrderByDescending(fc => fc.IncorrectCount - fc.CorrectCount).ToList();
                if (desiredCount != -1 && selected.Count > desiredCount)
                {
                    selected = selected.Take(desiredCount).ToList();
                }
            }
            else
            {
                selected = new List<Flashcard>(flashcards);
            }
            int n = selected.Count;
            for (int i = 0; i < n; i++)
            {
                int r = i + _random.Next(n - i);
                var temp = selected[r];
                selected[r] = selected[i];
                selected[i] = temp;
            }
            return selected;
        }

        [RelayCommand]
        public async Task LoadNextQuestionAsync()
        {
            try
            {
                Debug.WriteLine($"[LoadNextQuestionAsync] => Questions.Count={Questions.Count}, usedQuestions.Count={_usedQuestions.Count}");
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
                        Debug.WriteLine("[LoadNextQuestionAsync] => Switching to retry set, Questions.Count now=" + Questions.Count);
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
                    if (attempts > 100)
                    {
                        ExceptionHelper.LogError("⚠ Endlosschleife in LoadNextQuestionAsync erkannt!");
                        FeedbackMessage = "⚠ Fehler beim Laden der nächsten Frage!";
                        IsFeedbackVisible = true;
                        return;
                    }
                } while (_usedQuestions.Contains(nextQuestion.Id));

                Debug.WriteLine($"[LoadNextQuestionAsync] => Picked question ID={nextQuestion.Id}, Q='{nextQuestion.Question}'");
                _usedQuestions.Add(nextQuestion.Id);
                CurrentQuestion = nextQuestion;
                OnPropertyChanged(nameof(CurrentQuestion));
                OnPropertyChanged(nameof(CurrentQuestion.Question));
                OnPropertyChanged(nameof(CurrentQuestion.Answer));
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
            Debug.WriteLine($"[LoadFlashcard] => Questions.Count={Questions.Count}, usedQuestions.Count={_usedQuestions.Count}");
            if (Questions.Count == 0)
            {
                FeedbackMessage = "⚠ Keine Fragen in dieser Kategorie!";
                IsFeedbackVisible = true;
                return;
            }

            Flashcard nextFlashcard;
            int attempts = 0;
            do
            {
                nextFlashcard = Questions[_random.Next(Questions.Count)];
                attempts++;
                if (attempts > 100)
                {
                    ExceptionHelper.LogError("⚠ Endlosschleife in LoadFlashcard erkannt!");
                    FeedbackMessage = "⚠ Fehler beim Laden der Flashcard!";
                    IsFeedbackVisible = true;
                    return;
                }
            } while (_usedQuestions.Contains(nextFlashcard.Id) && _usedQuestions.Count < Questions.Count);

            _usedQuestions.Add(nextFlashcard.Id);
            string correctAnswer = DatabaseHelper.Instance.GetCorrectAnswerForFlashcard(nextFlashcard.Id);

            CurrentQuestion = new Flashcard
            {
                Id = nextFlashcard.Id,
                CategoryId = nextFlashcard.CategoryId,
                Question = nextFlashcard.Question,
                Answer = correctAnswer,
                CorrectCount = nextFlashcard.CorrectCount,
                IncorrectCount = nextFlashcard.IncorrectCount
            };

            Debug.WriteLine($"[LoadFlashcard] => ID={CurrentQuestion.Id}, Q='{CurrentQuestion.Question}'");
            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(CurrentQuestion.Answer));
            IsAnswerRevealed = false;
        }

        [RelayCommand]
        public void RevealAnswer()
        {
            Debug.WriteLine("[RevealAnswer] => Toggling answer reveal");
            if (!IsAnswerRevealed)
                IsAnswerRevealed = true;
            else
                LoadFlashcard();

            OnPropertyChanged(nameof(AnswerButtonText));
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

                var answers = DatabaseHelper.Instance.GetRandomAnswersForFlashcard(CurrentQuestion.Id);
                if (answers == null || answers.Count == 0)
                {
                    ExceptionHelper.LogError($"Keine Antworten für Frage {CurrentQuestion.Id} gefunden!");
                    FeedbackMessage = "⚠ Keine Antworten verfügbar!";
                    IsFeedbackVisible = true;
                    AnswerOptions = new ObservableCollection<FlashcardAnswer>();
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
                AnswerOptions = new ObservableCollection<FlashcardAnswer>();
            }
            OnPropertyChanged(nameof(AnswerButtonText));
        }

        private async Task ShowFinalStatisticsAsync()
        {
            try
            {
                Debug.WriteLine("[ShowFinalStatisticsAsync] => Setting IsSessionActive=false, IsFinalStatisticsVisible=true");
                IsSessionActive = false;
                IsFinalStatisticsVisible = true;
                OnPropertyChanged(nameof(IsSessionActive));
                OnPropertyChanged(nameof(IsFinalStatisticsVisible));

                OnPropertyChanged(nameof(TotalIncorrect));
                OnPropertyChanged(nameof(TotalCorrect));
                OnPropertyChanged(nameof(TotalAnswered));

                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("Fehler beim Anzeigen der Statistik.", ex);
            }
        }

        [RelayCommand]
        public async Task RestartSessionAsync()
        {
            Debug.WriteLine("[RestartSessionAsync] => Hiding stats, restarting session");
            IsFinalStatisticsVisible = false;
            IsSessionActive = true;
            OnPropertyChanged(nameof(IsFinalStatisticsVisible));
            OnPropertyChanged(nameof(IsSessionActive));
            await StartPracticeAsync();
        }

        [RelayCommand]
        public void CloseSession()
        {
            Debug.WriteLine("[CloseSession] => Final stats hidden, session inactive");
            IsFinalStatisticsVisible = false;
            IsSessionActive = false;
            OnPropertyChanged(nameof(IsFinalStatisticsVisible));
            OnPropertyChanged(nameof(IsSessionActive));
            ResetPracticeSession();
        }

        private void ResetPracticeSession()
        {
            Questions.Clear();
            CurrentQuestion = new Flashcard { Question = "Lade Frage..." };
            AnswerOptions.Clear();
            FeedbackMessage = "";
            IsFeedbackVisible = false;
            IsFinalStatisticsVisible = false;
            OnPropertyChanged(nameof(IsFinalStatisticsVisible));
            OnPropertyChanged(nameof(IsSessionActive));

            if (Categories != null && Categories.Count > 0)
                SelectedCategory = Categories.First();

            SelectedModeOption = AvailableModes.FirstOrDefault(m => m.Mode == PracticeMode.MultipleChoice)
                                  ?? new PracticeModeOption { Mode = PracticeMode.MultipleChoice, ModeName = "Multiple-Choice" };
            OnPropertyChanged(nameof(SelectedModeOption));

            Debug.WriteLine("[ResetPracticeSession] => Session reset, category and mode reset to defaults");
        }

        #endregion
    }
}
