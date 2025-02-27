using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NeoCardium.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Category> Categories { get; set; } = new();

        public MainPage()
        {
            this.InitializeComponent();
            DatabaseHelper.InitializeDatabase(); // Erstellt die Datenbank, falls sie nicht existiert
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
                }

                // UI-Thread verwenden, um Fehler zu vermeiden
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    Categories.Clear();
                    foreach (var category in categoriesFromDb)
                    {
                        Categories.Add(category);
                    }
                    CategoryListView.ItemsSource = null; // UI entkoppeln
                    CategoryListView.ItemsSource = Categories; // Neu setzen
                });

                Debug.WriteLine($"LoadCategories: {Categories.Count} Kategorien geladen.");
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Laden der Kategorien.", ex, this.XamlRoot);
            }
        }

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Category selectedCategory)
            {
                Frame.Navigate(typeof(FlashcardsPage), selectedCategory.Id);
            }
        }

        private void OpenContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.Flyout?.ShowAt(button);
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
                    // Prüfen, ob die Kategorie bereits existiert
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

    }
}
