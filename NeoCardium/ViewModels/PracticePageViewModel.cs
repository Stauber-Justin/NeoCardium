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
using Microsoft.UI.Xaml; // For XamlRoot access if needed
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.Views;
using Microsoft.UI.Dispatching;

namespace NeoCardium.ViewModels
{
    /// <summary>
    /// ViewModel for PracticePage.
    /// 
    /// Supports two practice modes:
    /// 
    /// 1. MultipleChoice mode ("Wer-Wird-Millionär"):
    ///    - On session start, a QuestionCountDialog is shown to choose 5, 10, 15 or all questions.
    ///    - Flashcards are fetched for the selected category and then sorted by a simplified SM2-like score
    ///      (score = IncorrectCount - CorrectCount). The top N (or all) are taken and shuffled.
    ///    - During the session, if an answer is wrong, the card is added to an incorrect list.
    ///      At the end, those cards are repeated (only wrong answers are counted for statistics).
    ///    - When a wrong answer is clicked, the correct answer is shown in the feedback.
    /// 
    /// 2. Classic Flashcard mode:
    ///    - Flashcards are simply shuffled.
    ///    - The user sees a question with a button to reveal the answer; after revealing, the same button becomes
    ///      "Nächste Frage" to load a new card.
    /// 
    /// Session flow:
    ///   Start Session -> (choose question count) -> fetch & select questions -> start session.
    ///   While active, process answers; when finished, show statistics with options to End or Retry.
    ///   "Stop Session" resets the session to state 0 (category selection visible, no active session).
    /// </summary>
    public partial class PracticePageViewModel : ObservableObject
    {
        // Fields for randomization and tracking questions.
        private readonly Random _random;
        private HashSet<int> _usedQuestions;
        private List<Flashcard> _incorrectQuestions;
        private int _totalCorrect;
        private int _totalIncorrect;

        // Data collections and current state.
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

        // Additional fields for new practice modes
        private string _typedUserAnswer = string.Empty;
        private int _timeLeft;
        private DispatcherTimer? _timer;

        // Flag to prevent multiple rapid answer processing.
        private bool _isProcessingAnswer;
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

        // Commands.
        public ICommand CheckAnswerAsync { get; }
        public ICommand CheckTypedAnswerAsync { get; }

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

            // Command to process answer clicks.
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
                if (SelectedMode == PracticeMode.Timed)
                    StopTimer();
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

            // Command for TypedAnswer mode
            CheckTypedAnswerAsync = new RelayCommand(async (_) =>
            {
                if (IsProcessingAnswer) return;
                IsProcessingAnswer = true;

                if (SelectedMode == PracticeMode.Timed)
                    StopTimer();

                string user = TypedUserAnswer?.Trim() ?? string.Empty;
                string correct = CurrentQuestion.Answer.Trim();
                bool correctFlag = string.Equals(user, correct, StringComparison.OrdinalIgnoreCase);

                if (correctFlag)
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
                    FeedbackMessage = $"⚠ Falsch ⚠\nRichtige Antwort: {correct}";
                    IsFeedbackVisible = true;
                    await Task.Delay(2000);
                }

                TypedUserAnswer = string.Empty;
                IsFeedbackVisible = false;
                await LoadNextQuestionAsync();
                IsProcessingAnswer = false;
            });

            Debug.WriteLine($"[PracticePageVM] Constructor => IsFinalStatisticsVisible={_isFinalStatisticsVisible}, IsSessionActive={_isSessionActive}");
        }

        /// <summary>
        /// Starts a new session.
        /// Opens a QuestionCountDialog to select how many questions to include,
        /// fetches flashcards for the selected category, and selects a subset based on the practice mode.
        /// Resets session statistics and loads the first question.
        /// </summary>
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

                // For now, desired count is set to all (-1).
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

                if (SelectedMode == PracticeMode.Flashcard)
                {
                    Debug.WriteLine("[StartPracticeAsync] => Mode=Flashcard, calling LoadFlashcard()");
                    LoadFlashcard();
                }
                else
                {
                    Debug.WriteLine($"[StartPracticeAsync] => Mode={SelectedMode}, calling LoadNextQuestionAsync()");
                    await LoadNextQuestionAsync();
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

        /// <summary>
        /// Helper method to select questions from all flashcards.
        /// For MultipleChoice mode: sorts flashcards by (IncorrectCount - CorrectCount),
        /// takes the top desiredCount (if desiredCount != -1), then shuffles.
        /// For Flashcard mode: simply shuffles all flashcards.
        /// </summary>
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
                    // Set the selected category to the first one by default.
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

        /// <summary>
        /// Toggles the session.
        /// If inactive, starts a new session.
        /// If active, stops the session and resets state.
        /// </summary>
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
                    StopTimer();
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

        /// <summary>
        /// Resets the current session state without clearing the selected category.
        /// Also resets the selected category to the first one (if available) and the mode to MultipleChoice.
        /// </summary>
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

            StopTimer();
            TypedUserAnswer = string.Empty;
            TimeLeft = 0;

            // Reset selection: always choose the first category, if any.
            if (Categories != null && Categories.Count > 0)
                SelectedCategory = Categories.First();

            // Reset the practice mode option to MultipleChoice.
            SelectedModeOption = AvailableModes.FirstOrDefault(m => m.Mode == PracticeMode.MultipleChoice)
                                  ?? new PracticeModeOption { Mode = PracticeMode.MultipleChoice, ModeName = "Multiple-Choice" };
            OnPropertyChanged(nameof(SelectedModeOption));

            Debug.WriteLine("[ResetPracticeSession] => Session reset, category and mode reset to defaults");
        }

        /// <summary>
        /// Restarts the session with the same questions (in a new randomized order).
        /// </summary>
        [RelayCommand]
        public async Task RestartSessionAsync()
        {
            Debug.WriteLine("[RestartSessionAsync] => Hiding stats, restarting session");
            StopTimer();
            IsFinalStatisticsVisible = false;
            IsSessionActive = true;
            OnPropertyChanged(nameof(IsFinalStatisticsVisible));
            OnPropertyChanged(nameof(IsSessionActive));
            await StartPracticeAsync();
        }

        /// <summary>
        /// Closes the session, hides final statistics, and resets state.
        /// </summary>
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

        /// <summary>
        /// Loads the next question for MultipleChoice mode.
        /// If all questions are used, checks for incorrect ones to iterate.
        /// </summary>
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

                if (SelectedMode == PracticeMode.MultipleChoice || SelectedMode == PracticeMode.Timed)
                {
                    LoadAnswers();
                }
                else if (SelectedMode == PracticeMode.TrueFalse)
                {
                    LoadTrueFalseAnswers();
                }
                else if (SelectedMode == PracticeMode.TypedAnswer)
                {
                    TypedUserAnswer = string.Empty;
                }

                if (SelectedMode == PracticeMode.Timed)
                {
                    StartTimer();
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.LogError("❌ Fehler in LoadNextQuestionAsync().", ex);
                FeedbackMessage = "⚠ Fehler beim Laden der nächsten Frage!";
                IsFeedbackVisible = true;
            }
        }

        /// <summary>
        /// For Classic Flashcard mode: loads a new flashcard randomly and updates the current question.
        /// </summary>
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

        /// <summary>
        /// Toggles answer reveal in Classic Flashcard mode.
        /// If already revealed, loads a new flashcard.
        /// </summary>
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

        private void StartTimer()
        {
            StopTimer();
            TimeLeft = 20;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            TimeLeft--;
            if (TimeLeft <= 0)
            {
                StopTimer();
                _totalIncorrect++;
                if (!_incorrectQuestions.Contains(CurrentQuestion))
                    _incorrectQuestions.Add(CurrentQuestion);
                CurrentQuestion.UpdateIncorrectCount();
                DatabaseHelper.Instance.UpdateFlashcardStats(CurrentQuestion.Id, false);
                FeedbackMessage = "⏰ Zeit abgelaufen!";
                IsFeedbackVisible = true;
                await Task.Delay(1000);
                IsFeedbackVisible = false;
                await LoadNextQuestionAsync();
            }
        }

        /// <summary>
        /// Loads answer options for the current question.
        /// If none are found, shows an error message.
        /// </summary>
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

        private void LoadTrueFalseAnswers()
        {
            AnswerOptions = new ObservableCollection<FlashcardAnswer>
            {
                new FlashcardAnswer
                {
                    AnswerText = "Wahr",
                    IsCorrect = CurrentQuestion.Answer.Trim().ToLower() == "true"
                },
                new FlashcardAnswer
                {
                    AnswerText = "Falsch",
                    IsCorrect = CurrentQuestion.Answer.Trim().ToLower() == "false"
                }
            };
        }

        /// <summary>
        /// Displays the final statistics overlay.
        /// </summary>
        private async Task ShowFinalStatisticsAsync()
        {
            try
            {
                Debug.WriteLine("[ShowFinalStatisticsAsync] => Setting IsSessionActive=false, IsFinalStatisticsVisible=true");
                StopTimer();
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

        // Public properties for data binding.
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

        // Text entered by the user in TypedAnswer mode
        public string TypedUserAnswer
        {
            get => _typedUserAnswer;
            set => SetProperty(ref _typedUserAnswer, value);
        }

        // Remaining time for Timed mode
        public int TimeLeft
        {
            get => _timeLeft;
            set => SetProperty(ref _timeLeft, value);
        }

        public ObservableCollection<PracticeModeOption> AvailableModes { get; } = new ObservableCollection<PracticeModeOption>
        {
            new PracticeModeOption { Mode = PracticeMode.MultipleChoice, ModeName = "Multiple-Choice" },
            new PracticeModeOption { Mode = PracticeMode.Flashcard, ModeName = "Karteikarten-Modus" },
            new PracticeModeOption { Mode = PracticeMode.TypedAnswer, ModeName = "Freitext" },
            new PracticeModeOption { Mode = PracticeMode.Timed, ModeName = "Zeitmodus" },
            new PracticeModeOption { Mode = PracticeMode.TrueFalse, ModeName = "Wahr/Falsch" }
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
    }
}
