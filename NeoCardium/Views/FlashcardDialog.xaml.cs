using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using NeoCardium.Models;
using NeoCardium.Helpers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace NeoCardium.Views
{
    public sealed partial class FlashcardDialog : ContentDialog
    {
        // VK_MENU (ALT) for checking alt-press specifically
        private const int VK_MENU = 0x12;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

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
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Hinzufügen der Antwort.", ex);
            }
        }

        // Called when user clicks primary button "Speichern"
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
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Überprüfen der Eingabe.", ex);
                args.Cancel = true;
            }
        }

        // Check if ALT is specifically pressed (for single-selection reset)
        private bool IsAltKeyPressed()
        {
            short altState = GetKeyState(VK_MENU);
            return (altState & 0x8000) != 0;
        }

        // ItemClick -> if ALT is pressed or no modifier is pressed => single selection
        // SHIFT/CTRL => rely on default extended selection behavior
        private void AnswersListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (IsAltKeyPressed())
            {
                // ALT+Click => reset to only clicked item
                AnswersListView.SelectedItems.Clear();
                AnswersListView.SelectedItems.Add(e.ClickedItem);
            }
            else if (!KeyboardHelper.IsModifierKeyPressed())
            {
                // No SHIFT/CTRL/ALT => single selection
                AnswersListView.SelectedItems.Clear();
                AnswersListView.SelectedItems.Add(e.ClickedItem);
            }
        }

        // Right-click context menu
        private void AnswersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is FlashcardAnswer)
            {
                // If the user right-clicked an item that isn't selected, select it alone
                if (!AnswersListView.SelectedItems.Contains(fe.DataContext))
                {
                    AnswersListView.SelectedItems.Clear();
                    AnswersListView.SelectedItems.Add(fe.DataContext);
                }

                int selectedCount = AnswersListView.SelectedItems.Count;
                var menu = new MenuFlyout();

                if (selectedCount == 1)
                {
                    // Single-selection context menu
                    var deleteItem = new MenuFlyoutItem { Text = "Löschen" };
                    deleteItem.Click += DeleteAnswer_Click;
                    menu.Items.Add(deleteItem);

                    var toggleItem = new MenuFlyoutItem { Text = "Correct/False" };
                    toggleItem.Click += ToggleCorrectAnswer_Click;
                    menu.Items.Add(toggleItem);
                }
                else
                {
                    // Multi-selection context menu
                    var multiDeleteItem = new MenuFlyoutItem { Text = "Auswahl Löschen" };
                    multiDeleteItem.Click += DeleteAnswer_Click;
                    menu.Items.Add(multiDeleteItem);
                }

                menu.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
                e.Handled = true;
            }
        }

        // Delete the currently selected answers (works for 1 or many)
        private void DeleteAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedAnswers = AnswersListView.SelectedItems.Cast<FlashcardAnswer>().ToList();
                if (selectedAnswers.Count == 0)
                {
                    ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Bitte wähle mindestens eine Antwort zum Löschen aus.");
                    return;
                }

                // Remove them all from the collection
                foreach (var ans in selectedAnswers)
                {
                    Answers.Remove(ans);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Entfernen der Antwort(en).", ex);
            }
        }

        // Toggle the IsCorrect property of the single selected answer
        private void ToggleCorrectAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnswersListView.SelectedItems.Count == 1 &&
                    AnswersListView.SelectedItems[0] is FlashcardAnswer answer)
                {
                    answer.IsCorrect = !answer.IsCorrect;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(ErrorInfoBar, "Fehler beim Umschalten des Korrektheitsstatus.", ex);
            }
        }
    }
}
