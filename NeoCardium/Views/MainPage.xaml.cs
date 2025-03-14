using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NeoCardium.Database;
using NeoCardium.Helpers;
using NeoCardium.Models;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace NeoCardium.Views
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Category> Categories { get; set; } = new();

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.InitializeDatabase();
            _ = LoadCategories();
        }

        private async Task LoadCategories()
        {
            try
            {
                var categoriesFromDb = DatabaseHelper.GetCategories();
                if (categoriesFromDb == null || categoriesFromDb.Count == 0)
                {
                    Debug.WriteLine("Keine Kategorien gefunden.");
                    return;
                }

                // Auf den UI-Thread
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    Categories.Clear();
                    foreach (var cat in categoriesFromDb)
                    {
                        Categories.Add(cat);
                    }
                    CategoryListView.ItemsSource = null;
                    CategoryListView.ItemsSource = Categories;
                });
            }
            catch (Exception ex)
            {
                await ExceptionHelper.ShowErrorDialogAsync("Fehler beim Laden der Kategorien.", ex, this.XamlRoot);
            }
        }

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Nur bei "normalem" Klick (ohne Shift/Ctrl) Auswahl zurücksetzen & navigieren
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

        private void CategoryListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 1) Prüfen, ob wir auf ein konkretes Category-Item geklickt haben
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is Category cat)
            {
                // Falls es noch nicht ausgewählt ist: Auswahl zurücksetzen & nur dieses Item
                if (!CategoryListView.SelectedItems.Contains(cat))
                {
                    CategoryListView.SelectedItems.Clear();
                    CategoryListView.SelectedItems.Add(cat);
                }
            }

            // 2) Jetzt Anzahl ausgewählter Items checken
            int selectedCount = CategoryListView.SelectedItems.Count;
            if (selectedCount == 0) return; // Nichts ausgewählt => kein Kontextmenü

            // 3) Passendes Flyout holen
            MenuFlyout flyout;
            if (selectedCount == 1)
            {
                flyout = (MenuFlyout)Resources["SingleCategoryContextFlyout"];
            }
            else
            {
                flyout = (MenuFlyout)Resources["MultiCategoryContextFlyout"];
            }

            // 4) Flyout am Mauszeiger (oder ListView) anzeigen
            flyout.ShowAt(CategoryListView, new FlyoutShowOptions
            {
                Position = e.GetPosition(CategoryListView)
            });

            e.Handled = true;
        }

        // Einzel-Menu: "Bearbeiten"
        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1 &&
                CategoryListView.SelectedItems[0] is Category selectedCategory)
            {
                var dialog = new CategoryDialog
                {
                    XamlRoot = this.XamlRoot,
                    EnteredCategoryName = selectedCategory.CategoryName
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary &&
                    !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
                {
                    bool success = DatabaseHelper.UpdateCategory(selectedCategory.Id, dialog.EnteredCategoryName);
                    if (!success)
                    {
                        await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht aktualisiert werden.", null, this.XamlRoot);
                    }
                    else
                    {
                        await LoadCategories();
                    }
                }
            }
        }

        // Einzel-Menu: "Löschen"
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1 &&
                CategoryListView.SelectedItems[0] is Category selectedCategory)
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
                    }
                    else
                    {
                        await LoadCategories();
                    }
                }
            }
        }

        // Multi-Menu: "Extract"
        private void ExtractSelectedCategories_Click(object sender, RoutedEventArgs e)
        {
            var selected = CategoryListView.SelectedItems.Cast<Category>().ToList();
            Debug.WriteLine($"Extract: {selected.Count} Kategorien (Placeholder)...");
            // ... Deine "Extract"-Logik
        }

        // Multi-Menu: "Auswahl löschen"
        private async void DeleteSelectedCategories_Click(object sender, RoutedEventArgs e)
        {
            var selected = CategoryListView.SelectedItems.Cast<Category>().ToList();
            if (selected.Count == 0) return;

            string msg = (selected.Count == 1)
                ? $"Möchtest du die Kategorie '{selected[0].CategoryName}' wirklich löschen?"
                : $"Möchtest du die {selected.Count} ausgewählten Kategorien wirklich löschen?";

            var confirmDialog = new ContentDialog
            {
                Title = "Kategorien löschen",
                Content = msg,
                PrimaryButtonText = "Löschen",
                CloseButtonText = "Abbrechen",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                foreach (var cat in selected)
                {
                    DatabaseHelper.DeleteCategory(cat.Id);
                }
                await LoadCategories();
            }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryDialog { XamlRoot = this.XamlRoot };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary &&
                !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
            {
                if (DatabaseHelper.CategoryExists(dialog.EnteredCategoryName))
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Diese Kategorie existiert bereits.", null, this.XamlRoot);
                    return;
                }

                bool success = DatabaseHelper.AddCategory(dialog.EnteredCategoryName);
                if (!success)
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht gespeichert werden.", null, this.XamlRoot);
                }
                else
                {
                    await LoadCategories();
                }
            }
        }
    }
}
