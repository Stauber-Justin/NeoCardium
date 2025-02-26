using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.ObjectModel;
using NeoCardium.Models;

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
            if (!string.IsNullOrWhiteSpace(AnswerTextBox.Text))
            {
                Answers.Add(new FlashcardAnswer
                {
                    AnswerText = AnswerTextBox.Text,
                    IsCorrect = IsCorrectCheckBox.IsChecked == true
                });

                AnswerTextBox.Text = ""; // Eingabe leeren
                IsCorrectCheckBox.IsChecked = false;
            }
        }

        private void RemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (AnswersListView.SelectedItem is FlashcardAnswer selectedAnswer)
            {
                Answers.Remove(selectedAnswer);
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(Question) || Answers.Count == 0)
            {
                args.Cancel = true; // Dialog bleibt offen, wenn keine Antworten eingegeben wurden
            }
        }
    }
}
