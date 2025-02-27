using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NeoCardium.Models;
using NeoCardium.Helpers;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardDialog : ContentDialog
    {
        public string Question
        {
            get => QuestionTextBox.Text;
            set => QuestionTextBox.Text = value;
        }

        public ObservableCollection<FlashcardAnswer> Answers { get; private set; } = new();

        public FlashcardDialog()
        {
            this.InitializeComponent();
            AnswersListView.ItemsSource = Answers;
        }

        private async void AddAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AnswerTextBox.Text))
                {
                    Answers.Add(new FlashcardAnswer
                    {
                        AnswerText = AnswerTextBox.Text.Trim(),
                        IsCorrect = IsCorrectCheckBox.IsChecked == true
                    });

                    AnswerTextBox.Text = ""; // Eingabe leeren
                    IsCorrectCheckBox.IsChecked = false;
                }
                else
                {
                    ExceptionHelper.ShowError(ErrorInfoBar, "Antwort darf nicht leer sein.");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowError(ErrorInfoBar, $"Fehler beim Hinzufügen der Antwort: {ex.Message}");
            }
        }

        private async void RemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnswersListView.SelectedItem is FlashcardAnswer selectedAnswer)
                {
                    Answers.Remove(selectedAnswer);
                }
                else
                {
                    ExceptionHelper.ShowError(ErrorInfoBar, "Bitte wähle eine Antwort zum Löschen aus.");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowError(ErrorInfoBar, $"Fehler beim Entfernen der Antwort: {ex.Message}");
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Question))
                {
                    ExceptionHelper.ShowError(ErrorInfoBar, "Die Frage darf nicht leer sein.");
                    args.Cancel = true;
                    return;
                }

                if (Answers.Count == 0)
                {
                    ExceptionHelper.ShowError(ErrorInfoBar, "Mindestens eine Antwort muss eingegeben werden.");
                    args.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowError(ErrorInfoBar, $"Fehler beim Überprüfen der Eingabe: {ex.Message}");
                args.Cancel = true;
            }
        }
    }
}
