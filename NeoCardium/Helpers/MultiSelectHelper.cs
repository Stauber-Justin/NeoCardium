using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using NeoCardium.Models;
using Windows.System;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using NeoCardium.Database;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using System.Text;

namespace NeoCardium.Helpers
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    public static class MultiSelectBehavior
    {
        #region Attached Properties

        public static readonly DependencyProperty EnableMultiSelectProperty =
            DependencyProperty.RegisterAttached(
                "EnableMultiSelect",
                typeof(bool),
                typeof(MultiSelectBehavior),
                new PropertyMetadata(false, OnEnableMultiSelectChanged));

        public static bool GetEnableMultiSelect(DependencyObject obj) =>
            (bool)obj.GetValue(EnableMultiSelectProperty);
        public static void SetEnableMultiSelect(DependencyObject obj, bool value) =>
            obj.SetValue(EnableMultiSelectProperty, value);

        public static readonly DependencyProperty DeleteAllCommandProperty =
            DependencyProperty.RegisterAttached(
                "DeleteAllCommand",
                typeof(System.Windows.Input.ICommand),
                typeof(MultiSelectBehavior),
                new PropertyMetadata(null));

        public static System.Windows.Input.ICommand GetDeleteAllCommand(DependencyObject obj) =>
            (System.Windows.Input.ICommand)obj.GetValue(DeleteAllCommandProperty);
        public static void SetDeleteAllCommand(DependencyObject obj, System.Windows.Input.ICommand value) =>
            obj.SetValue(DeleteAllCommandProperty, value);

        public static readonly DependencyProperty ExtractCommandProperty =
            DependencyProperty.RegisterAttached(
                "ExtractCommand",
                typeof(System.Windows.Input.ICommand),
                typeof(MultiSelectBehavior),
                new PropertyMetadata(null));

        public static System.Windows.Input.ICommand GetExtractCommand(DependencyObject obj) =>
            (System.Windows.Input.ICommand)obj.GetValue(ExtractCommandProperty);
        public static void SetExtractCommand(DependencyObject obj, System.Windows.Input.ICommand value) =>
            obj.SetValue(ExtractCommandProperty, value);

        #endregion

        #region Private State

        private class MultiSelectState
        {
            public bool IsSelecting { get; set; }
            public int StartIndex { get; set; }
        }
        private static readonly Dictionary<ListView, MultiSelectState> _states = new Dictionary<ListView, MultiSelectState>();

        #endregion

        #region Attached Property Changed

        private static void OnEnableMultiSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                bool enabled = (bool)e.NewValue;
                if (enabled)
                {
                    listView.SelectionMode = ListViewSelectionMode.Multiple;
                    listView.PointerPressed += ListView_PointerPressed;
                    listView.PointerMoved += ListView_PointerMoved;
                    listView.PointerReleased += ListView_PointerReleased;
                    listView.RightTapped += ListView_RightTapped;
                    _states[listView] = new MultiSelectState();
                }
                else
                {
                    listView.PointerPressed -= ListView_PointerPressed;
                    listView.PointerMoved -= ListView_PointerMoved;
                    listView.PointerReleased -= ListView_PointerReleased;
                    listView.RightTapped -= ListView_RightTapped;
                    _states.Remove(listView);
                }
            }
        }

        #endregion

        #region Pointer Event Handlers

        private static void ListView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ListView listView && _states.TryGetValue(listView, out var state))
            {
                var point = e.GetCurrentPoint(listView);
                if (point.Properties.IsLeftButtonPressed && (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
                {
                    if (e.OriginalSource is FrameworkElement element && element.DataContext != null)
                    {
                        int index = listView.Items.IndexOf(element.DataContext);
                        if (index >= 0)
                        {
                            state.IsSelecting = true;
                            state.StartIndex = index;
                            listView.SelectedItems.Clear();
                            listView.SelectedItems.Add(element.DataContext);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private static void ListView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ListView listView && _states.TryGetValue(listView, out var state))
            {
                if (state.IsSelecting)
                {
                    var point = e.GetCurrentPoint(listView).Position;
                    var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, listView);
                    object? currentItem = null;
                    foreach (var elem in elements)
                    {
                        if (elem is ListViewItem lvi && lvi.DataContext != null)
                        {
                            currentItem = lvi.DataContext;
                            break;
                        }
                    }
                    if (currentItem != null)
                    {
                        int currentIndex = listView.Items.IndexOf(currentItem);
                        if (currentIndex >= 0)
                        {
                            listView.SelectedItems.Clear();
                            int start = Math.Min(state.StartIndex, currentIndex);
                            int end = Math.Max(state.StartIndex, currentIndex);
                            for (int i = start; i <= end; i++)
                            {
                                listView.SelectedItems.Add(listView.Items[i]);
                            }
                        }
                    }
                    e.Handled = true;
                }
            }
        }

        private static void ListView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ListView listView && _states.TryGetValue(listView, out var state))
            {
                if (state.IsSelecting)
                {
                    state.IsSelecting = false;
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region RightTapped Handler

        private static async void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is ListView listView)
            {
                // Hier verwenden wir GetPosition statt GetCurrentPoint
                var point = e.GetPosition(listView);
                var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, listView);
                object? tappedItem = null;
                FrameworkElement? tappedElement = null;
                foreach (var elem in elements)
                {
                    if (elem is ListViewItem lvi && lvi.DataContext != null)
                    {
                        tappedItem = lvi.DataContext;
                        tappedElement = lvi;
                        break;
                    }
                }
                if (tappedItem == null || tappedElement == null) return;

                // Wenn das angeklickte Element nicht bereits ausgewählt ist, Auswahl zurücksetzen und nur dieses Element auswählen.
                if (!listView.SelectedItems.Contains(tappedItem))
                {
                    listView.SelectedItems.Clear();
                    listView.SelectedItems.Add(tappedItem);
                }

                // Erstelle das Kontextmenü für Mehrfachauswahl.
                MenuFlyout flyout = new MenuFlyout();

                MenuFlyoutItem extractItem = new MenuFlyoutItem { Text = "Export Selected" };
                extractItem.Click += async (s, args) =>
                {
                    var command = GetExtractCommand(listView);
                    if (command != null && command.CanExecute(listView.SelectedItems))
                    {
                        command.Execute(listView.SelectedItems);
                    }
                    else
                    {
                        var categories = listView.SelectedItems.Cast<Category>().ToList();
                        await ExportCategoriesAsync(categories, listView.XamlRoot);
                    }
                };
                flyout.Items.Add(extractItem);

                MenuFlyoutItem deleteAllItem = new MenuFlyoutItem { Text = "Delete All" };
                deleteAllItem.Click += async (s, args) =>
                {
                    var command = GetDeleteAllCommand(listView);
                    if (command != null && command.CanExecute(listView.SelectedItems))
                    {
                        command.Execute(listView.SelectedItems);
                    }
                    else
                    {
                        var selectedCategories = listView.SelectedItems.Cast<Category>().ToList();
                        if (selectedCategories.Count > 0)
                        {
                            string message = selectedCategories.Count == 1
                                ? $"Möchtest du die Kategorie '{selectedCategories[0].CategoryName}' wirklich löschen?"
                                : $"Möchtest du die {selectedCategories.Count} ausgewählten Kategorien wirklich löschen?";
                            ContentDialog confirmDialog = new ContentDialog
                            {
                                Title = "Kategorien löschen",
                                Content = message,
                                PrimaryButtonText = "Löschen",
                                CloseButtonText = "Abbrechen",
                                XamlRoot = listView.XamlRoot
                            };
                            var result = await confirmDialog.ShowAsync();
                            if (result == ContentDialogResult.Primary)
                            {
                                foreach (var category in selectedCategories)
                                {
                                    DatabaseHelper.Instance.DeleteCategory(category.Id);
                                }
                                // Hier können Sie z. B. ein Event feuern oder die UI anderweitig aktualisieren.
                            }
                        }
                    }
                };
                flyout.Items.Add(deleteAllItem);

                flyout.ShowAt(tappedElement, new FlyoutShowOptions { Position = e.GetPosition(tappedElement) });
                e.Handled = true;
            }
            await Task.CompletedTask;
        }

        private static async Task ExportCategoriesAsync(List<Category> categories, XamlRoot xamlRoot)
        {
            if (categories == null || categories.Count == 0)
                return;

            try
            {
                StringBuilder exportBuilder = new StringBuilder();
                foreach (var category in categories)
                {
                    var flashcards = DatabaseHelper.Instance.GetFlashcardsByCategory(category.Id);
                    foreach (var flashcard in flashcards)
                    {
                        exportBuilder.Append($"{category.CategoryName}@next@{flashcard.Question}");
                        var answers = DatabaseHelper.Instance.GetAnswersByFlashcard(flashcard.Id);
                        foreach (var answer in answers)
                        {
                            string answerField = answer.IsCorrect ? $"@correct@{answer.AnswerText}" : answer.AnswerText;
                            exportBuilder.Append($"@next@{answerField}");
                        }
                        exportBuilder.Append("@end@");
                        exportBuilder.AppendLine();
                    }
                }

                string exportContent = exportBuilder.ToString();

                FileSavePicker savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                savePicker.FileTypeChoices.Add("Custom CSV file", new List<string> { ".csv" });
                savePicker.SuggestedFileName = "export.csv";
                var handle = WindowNative.GetWindowHandle(App._mainWindow);
                InitializeWithWindow.Initialize(savePicker, handle);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, exportContent);
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Exportieren der ausgewählten Kategorien.", ex, xamlRoot);
            }
        }

        #endregion
    }
}