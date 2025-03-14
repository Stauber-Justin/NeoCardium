using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardDialogBatch : ContentDialog
    {
        public int CategoryId { get; set; }

        // This is our temporary parse result
        private List<(string question, List<FlashcardAnswer> answers)> _parsedFlashcards
            = new List<(string question, List<FlashcardAnswer> answers)>();

        public FlashcardDialogBatch()
        {
            this.InitializeComponent();
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorInfoBar.IsOpen = false;
            _parsedFlashcards.Clear();
            PreviewListView.ItemsSource = null;

            var lines = BatchInputTextBox.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse each line
            foreach (var line in lines)
            {
                // example format: "Question?Answer1,Answer2,Answer3"
                var parts = line.Split('?');
                if (parts.Length < 2)
                {
                    // invalid line -> skip or show error
                    Debug.WriteLine($"[WARN] Invalid line: {line}");
                    continue;
                }
                string question = parts[0].Trim();
                var answersPart = parts[1].Split(',');
                var answers = new List<FlashcardAnswer>();

                for (int i = 0; i < answersPart.Length; i++)
                {
                    bool isLast = (i == answersPart.Length - 1);
                    answers.Add(new FlashcardAnswer
                    {
                        AnswerText = answersPart[i].Trim(),
                        IsCorrect = isLast // last one is correct
                    });
                }

                if (!string.IsNullOrWhiteSpace(question) && answers.Any())
                {
                    _parsedFlashcards.Add((question, answers));
                }
            }

            // Show preview
            var previewLines = _parsedFlashcards.Select(p =>
                $"{p.question} -> {string.Join(", ", p.answers.Select(a => a.AnswerText))} (Last is correct)"
            ).ToList();

            PreviewListView.ItemsSource = previewLines;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ErrorInfoBar.IsOpen = false;
            try
            {
                if (!_parsedFlashcards.Any())
                {
                    ErrorInfoBar.Message = "Keine gültigen Eingaben erkannt. ...";
                    ErrorInfoBar.IsOpen = true;
                    args.Cancel = true;
                    return;
                }

                // Insert into DB (synchronously)
                foreach (var (question, answers) in _parsedFlashcards)
                {
                    bool success = DatabaseHelper.Instance.AddFlashcard(CategoryId, question, answers, out string errorMsg);
                    if (!success)
                    {
                        if (errorMsg == "duplicate")
                        {
                            ErrorInfoBar.Message = $"Duplikat gefunden: '{question}'";
                            ErrorInfoBar.IsOpen = true;
                            args.Cancel = true;
                            return;
                        }
                        else
                        {
                            ErrorInfoBar.Message = $"Fehler beim Erstellen: '{question}'";
                            ErrorInfoBar.IsOpen = true;
                            args.Cancel = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = "Unerwarteter Fehler beim Batch-Erstellen: " + ex.Message;
                ErrorInfoBar.IsOpen = true;
                args.Cancel = true;
            }
        }
    }
}
