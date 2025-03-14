using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
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

        private void AddAnswer_Click(object sender, RoutedEventArgs e)
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
                    ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Antwort darf nicht leer sein.");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Hinzuf�gen der Antwort.", ex);
            }
        }

        private void RemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnswersListView.SelectedItem is FlashcardAnswer selectedAnswer)
                {
                    Answers.Remove(selectedAnswer);
                }
                else
                {
                    ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Bitte w�hle eine Antwort zum L�schen aus.");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Entfernen der Antwort.", ex);
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Question))
                {
                    ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Die Frage darf nicht leer sein.");
                    args.Cancel = true;
                    return;
                }

                if (Answers.Count == 0)
                {
                    ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Mindestens eine Antwort muss eingegeben werden.");
                    args.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim �berpr�fen der Eingabe.", ex);
                args.Cancel = true;
            }
        }
    }
}
