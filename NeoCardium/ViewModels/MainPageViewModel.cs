using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Xaml;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace NeoCardium.ViewModels
{
    public class ObservableObject : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;
            storage = value;
            if (propertyName != null)
                OnPropertyChanged(propertyName);
            return true;
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainPageViewModel : ObservableObject
    {
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        public ICommand DeleteCategoryCommand { get; }
        public ICommand DeleteSelectedCategoriesCommand { get; }
        public ICommand ExportCategoriesCommand { get; }
        public ICommand ImportCategoriesCommand { get; } // <--- NEW

        public MainPageViewModel()
        {
            DeleteCategoryCommand = new RelayCommand(async (param) => await DeleteCategoryAsync(param as Category));
            DeleteSelectedCategoriesCommand = new RelayCommand(async (param) => await DeleteSelectedCategoriesAsync(param as IEnumerable<Category>));
            ExportCategoriesCommand = new RelayCommand(async (param) => await ExportCategoriesAsync(param as IEnumerable<Category>));
            ImportCategoriesCommand = new RelayCommand(async (_) => await ImportCategoriesAsync()); // <--- NEW
        }

        public async Task LoadCategoriesAsync()
        {
            var cats = DatabaseHelper.Instance.GetCategories();
            Categories.Clear();
            foreach (var cat in cats)
            {
                Categories.Add(cat);
            }
            await Task.CompletedTask;
        }

        private async Task DeleteCategoryAsync(Category? category)
        {
            if (category == null)
                return;
            DatabaseHelper.Instance.DeleteCategory(category.Id);
            await LoadCategoriesAsync();
        }

        private async Task DeleteSelectedCategoriesAsync(IEnumerable<Category>? selectedCategories)
        {
            if (selectedCategories == null || !selectedCategories.Any())
                return;

            foreach (var cat in selectedCategories.ToList())
            {
                DatabaseHelper.Instance.DeleteCategory(cat.Id);
            }
            await LoadCategoriesAsync();
        }

        private async Task ExportCategoriesAsync(IEnumerable<Category>? selectedCategories)
        {
            if (selectedCategories == null || !selectedCategories.Any())
                return;

            try
            {
                StringBuilder exportBuilder = new StringBuilder();

                // Iterate through each category and its flashcards.
                foreach (var category in selectedCategories)
                {
                    var flashcards = DatabaseHelper.Instance.GetFlashcardsByCategory(category.Id);
                    foreach (var flashcard in flashcards)
                    {
                        // Start with the category name and question using custom column delimiter.
                        exportBuilder.Append($"{category.CategoryName}@next@{flashcard.Question}");

                        // Get answers for the flashcard.
                        var answers = DatabaseHelper.Instance.GetAnswersByFlashcard(flashcard.Id);
                        foreach (var answer in answers)
                        {
                            // For correct answers, prefix with "@correct@"
                            string answerField = answer.IsCorrect ? $"@correct@{answer.AnswerText}" : answer.AnswerText;
                            exportBuilder.Append($"@next@{answerField}");
                        }

                        // End the flashcard row with custom row delimiter.
                        exportBuilder.Append("@end@");
                        exportBuilder.AppendLine();
                    }
                }

                string exportContent = exportBuilder.ToString();

                // Create and configure the FileSavePicker.
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Custom CSV file", new System.Collections.Generic.List<string>() { ".csv" });
                savePicker.SuggestedFileName = "export.csv";

                // For WinUI3 Desktop, set the window handle.
                var windowHandle = WindowNative.GetWindowHandle(App._mainWindow);
                InitializeWithWindow.Initialize(savePicker, windowHandle);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, exportContent);
                }
            }
            catch (System.Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Exportieren der ausgewählten Kategorien.", ex, null);
            }
        }

        // ---------------------------------------------------------------------
        // NEW: Import logic using our custom CSV format
        // ---------------------------------------------------------------------
        private async Task ImportCategoriesAsync()
        {
            try
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".csv");  // or .txt or any extension

                // For WinUI3 desktop:
                var windowHandle = WindowNative.GetWindowHandle(App._mainWindow);
                InitializeWithWindow.Initialize(openPicker, windowHandle);

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file == null)
                {
                    // user canceled
                    return;
                }

                string fileContent = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Die ausgewählte Datei ist leer oder ungültig.");
                    return;
                }

                // We'll split by newlines. Each line should end with "@end@"
                // But let's be safe: we only treat lines that contain '@end@'.
                var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (!line.Contains("@end@"))
                    {
                        // skip or show warning?
                        continue;
                    }

                    // Remove the trailing "@end@"
                    // If there's any text after @end@, we can remove it.
                    int endIndex = line.IndexOf("@end@");
                    string trimmedLine = line.Substring(0, endIndex);

                    // Now split by "@next@" to get fields
                    var fields = trimmedLine.Split(new[] { "@next@" }, StringSplitOptions.None);

                    if (fields.Length < 2)
                    {
                        // We at least need CategoryName + Question
                        continue;
                    }

                    string categoryName = fields[0].Trim();
                    string question = fields[1].Trim();

                    // The rest are answers
                    var answerFields = fields.Skip(2).ToList();

                    // 1) Ensure the category exists or create it
                    int categoryId = EnsureCategory(categoryName);

                    // 2) Add the flashcard (if not duplicate)
                    var flashcardAnswers = new List<FlashcardAnswer>();
                    foreach (var ansField in answerFields)
                    {
                        if (ansField.StartsWith("@correct@"))
                        {
                            string answerText = ansField.Substring("@correct@".Length);
                            flashcardAnswers.Add(new FlashcardAnswer
                            {
                                AnswerText = answerText,
                                IsCorrect = true
                            });
                        }
                        else
                        {
                            flashcardAnswers.Add(new FlashcardAnswer
                            {
                                AnswerText = ansField,
                                IsCorrect = false
                            });
                        }
                    }

                    bool success = DatabaseHelper.Instance.AddFlashcard(categoryId, question, flashcardAnswers, out string errorMsg);
                    if (!success)
                    {
                        if (errorMsg == "duplicate")
                        {
                            // Optionally skip or notify user about duplicates
                            // We'll just skip for now
                            continue;
                        }
                        else
                        {
                            // Some unknown error
                            await ExceptionHelper.ShowErrorDialogAsync(
                                $"Karteikarte \"{question}\" konnte nicht importiert werden. Fehler: {errorMsg}"
                            );
                        }
                    }
                }

                // Refresh UI
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Importieren der Datei.", ex);
            }
        }

        /// <summary>
        /// Checks if a category with the given name exists. If not, creates it.
        /// Returns the category's ID.
        /// </summary>
        private int EnsureCategory(string categoryName)
        {
            // If it already exists, fetch it
            var existingCats = DatabaseHelper.Instance.GetCategories();
            var existing = existingCats.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing.Id;
            }

            // Otherwise, create it
            bool created = DatabaseHelper.Instance.AddCategory(categoryName);
            if (!created)
            {
                // If something went wrong, we might just do a fallback
                // but for now we re-fetch categories to see if it actually got created
                existingCats = DatabaseHelper.Instance.GetCategories();
                existing = existingCats.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (existing != null) return existing.Id;
            }

            // Retrieve the newly created category
            var allCats = DatabaseHelper.Instance.GetCategories();
            var newCat = allCats.FirstOrDefault(c => c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (newCat == null)
            {
                // fallback
                return -1;
            }

            return newCat.Id;
        }
    }
}
