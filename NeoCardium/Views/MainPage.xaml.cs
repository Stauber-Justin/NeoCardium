using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.Models;

namespace NeoCardium.Views
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Category> Categories { get; set; } = new();

        // Neue Befehle für die Multi-Select-Aktionen
        public ICommand DeleteSelectedCategoriesCommand { get; }
        public ICommand ExtractSelectedCategoriesCommand { get; }

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;

            // Befehle mit RelayCommand initialisieren
            DeleteSelectedCategoriesCommand = new RelayCommand(DeleteSelectedCategories);
            ExtractSelectedCategoriesCommand = new RelayCommand(ExtractSelectedCategories);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.InitializeDatabase(); // Erstellt die Datenbank, falls nicht vorhanden
            _ = LoadCategories();
        }

        private async Task LoadCategories()
        {
            try
            {
                var categoriesFromDb = DatabaseHelper.GetCategories();
                if (categoriesFromDb == null || categoriesFromDb.Count == 0)
                {
                    Debug.WriteLine("LoadCategories: Keine Kategorien gefunden.");
                    return;
                }

                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    Categories.Clear();
                    foreach (var category in categoriesFromDb)
                    {
                        Categories.Add(category);
                    }
                    CategoryListView.ItemsSource = null;
                    CategoryListView.ItemsSource = Categories;
                });

                Debug.WriteLine($"LoadCategories: {Categories.Count} Kategorien geladen.");
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Laden der Kategorien.", ex, this.XamlRoot);
            }
        }

        // Bei einem normalen Klick (ohne Modifier) wird die Auswahl zurückgesetzt und navigiert.
        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!KeyboardHelper.IsModifierKeyPressed())
            {
                CategoryListView.SelectedItems.Clear();
                CategoryListView.SelectedItems.Add(e.ClickedItem);
                if (e.ClickedItem is Category selectedCategory)
                {
                    Frame.Navigate(typeof(FlashcardsPage), selectedCategory.Id);
                }
            }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var categoryDialog = new CategoryDialog();
                categoryDialog.XamlRoot = this.XamlRoot;

                var dialogResult = await categoryDialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(categoryDialog.EnteredCategoryName))
                {
                    if (DatabaseHelper.CategoryExists(categoryDialog.EnteredCategoryName))
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Diese Kategorie existiert bereits.", null, this.XamlRoot);
                        return;
                    }

                    bool success = DatabaseHelper.AddCategory(categoryDialog.EnteredCategoryName);
                    Debug.WriteLine($"MainPage: AddCategory success = {success}");

                    if (!success)
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht gespeichert werden.", null, this.XamlRoot);
                        return;
                    }

                    await LoadCategories();
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Erstellen der Kategorie.", ex, this.XamlRoot);
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as MenuFlyoutItem)?.DataContext is Category selectedCategory)
                {
                    var confirmDialog = new ContentDialog
                    {
                        Title = "Kategorie löschen",
                        Content = $"Möchtest du die Kategorie '{selectedCategory.CategoryName}' wirklich löschen?",
                        PrimaryButtonText = "Löschen",
                        CloseButtonText = "Abbrechen",
                        XamlRoot = this.XamlRoot
                    };

                    var result = await confirmDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        bool success = DatabaseHelper.DeleteCategory(selectedCategory.Id);
                        if (!success)
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht gelöscht werden.", null, this.XamlRoot);
                            return;
                        }

                        await LoadCategories();
                    }
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Löschen der Kategorie.", ex, this.XamlRoot);
            }
        }

        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as MenuFlyoutItem)?.DataContext is Category selectedCategory)
                {
                    var dialog = new CategoryDialog();
                    dialog.EnteredCategoryName = selectedCategory.CategoryName;
                    dialog.XamlRoot = this.XamlRoot;

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
                    {
                        bool success = DatabaseHelper.UpdateCategory(selectedCategory.Id, dialog.EnteredCategoryName);
                        if (!success)
                        {
                            await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht aktualisiert werden.", null, this.XamlRoot);
                            return;
                        }

                        await LoadCategories();
                    }
                }
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Bearbeiten der Kategorie.", ex, this.XamlRoot);
            }
        }

        // Handler für den Delete-All-Befehl bei Mehrfachauswahl.
        private async void DeleteSelectedCategories(object? parameter)
        {
            if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0)
            {
                var categories = selectedItems.Cast<Category>().ToList();
                string message = categories.Count == 1
                    ? $"Möchtest du die Kategorie '{categories[0].CategoryName}' wirklich löschen?"
                    : $"Möchtest du die {categories.Count} ausgewählten Kategorien wirklich löschen?";

                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "Kategorien löschen",
                    Content = message,
                    PrimaryButtonText = "Löschen",
                    CloseButtonText = "Abbrechen",
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    foreach (var category in categories)
                    {
                        DatabaseHelper.DeleteCategory(category.Id);
                    }
                    await LoadCategories();
                }
            }
        }

        // Handler für den Extract-Befehl (Platzhalter).
        private void ExtractSelectedCategories(object? parameter)
        {
            Debug.WriteLine("ExtractSelectedCategories command executed (placeholder).");
        }
    }
}
