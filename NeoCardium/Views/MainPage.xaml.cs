using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NeoCardium.Models;
using NeoCardium.ViewModels;
using NeoCardium.Helpers;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace NeoCardium.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            NeoCardium.Database.DatabaseHelper.InitializeDatabase();
            await ViewModel.LoadCategoriesAsync();
        }

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!KeyboardHelper.IsModifierKeyPressed())
            {
                CategoryListView.SelectedItems.Clear();
                CategoryListView.SelectedItems.Add(e.ClickedItem);
                if (GetCategoryFromItem(e.ClickedItem) is Category cat)
                {
                    Frame.Navigate(typeof(FlashcardsPage), cat.Id);
                }
            }
        }

        private void CategoryListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && GetCategoryFromItem(fe.DataContext) is Category cat)
            {
                // Falls das getroffene Element noch nicht selektiert ist, wähle es aus
                if (!CategoryListView.SelectedItems.Cast<object>().Any(item => GetCategoryFromItem(item)?.Id == cat.Id))
                {
                    CategoryListView.SelectedItems.Clear();
                    CategoryListView.SelectedItems.Add(cat);
                }
            }

            int count = CategoryListView.SelectedItems.Count;
            if (count == 0) return;

            // Wähle das passende Flyout basierend auf der Anzahl der selektierten Kategorien
            MenuFlyout flyout = (count == 1) ?
                (MenuFlyout)Resources["SingleCategoryContextFlyout"] :
                (MenuFlyout)Resources["MultiCategoryContextFlyout"];

            flyout.ShowAt(CategoryListView, new FlyoutShowOptions { Position = e.GetPosition(CategoryListView) });
            e.Handled = true;
        }

        // Hilfsmethode: Extrahiert aus einem Element das Category-Objekt
        private Category? GetCategoryFromItem(object? item)
        {
            if (item is Category cat)
                return cat;
            if (item is ListViewItem lvi && lvi.DataContext is Category cat2)
                return cat2;
            return null;
        }

        // Gemeinsamer Bestätigungsdialog für das Löschen (DRY)
        private async Task<ContentDialogResult> ShowDeleteConfirmationDialogAsync(IList<Category> categories)
        {
            string message = categories.Count == 1
                ? $"Möchtest du die Kategorie '{categories[0].CategoryName}' wirklich löschen?"
                : $"Möchtest du die {categories.Count} ausgewählten Kategorien wirklich löschen?";

            var confirmDialog = new ContentDialog
            {
                Title = "Kategorien löschen",
                Content = message,
                PrimaryButtonText = "Löschen",
                CloseButtonText = "Abbrechen",
                XamlRoot = this.XamlRoot
            };

            return await confirmDialog.ShowAsync();
        }

        // Single-Menu "Löschen" – verwendet den gemeinsamen ConfirmDialog
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1)
            {
                var cat = GetCategoryFromItem(CategoryListView.SelectedItems[0]);
                if (cat != null)
                {
                    var list = new List<Category> { cat };
                    var result = await ShowDeleteConfirmationDialogAsync(list);
                    if (result == ContentDialogResult.Primary)
                    {
                        ViewModel.DeleteCategoryCommand.Execute(cat);
                    }
                }
            }
        }

        // Multi-Menu "Auswahl löschen" – verwendet ebenfalls den gemeinsamen ConfirmDialog
        private async void DeleteSelectedCategories_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategories = CategoryListView.SelectedItems
                .Cast<object>()
                .Select(item => GetCategoryFromItem(item))
                .Where(cat => cat != null)
                .Cast<Category>()
                .ToList();

            if (selectedCategories.Count == 0)
                return;

            var result = await ShowDeleteConfirmationDialogAsync(selectedCategories);
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteSelectedCategoriesCommand.Execute(selectedCategories);
            }
        }

        // Multi-Menu "Extract (Placeholder)"
        private void ExtractSelectedCategories_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategories = CategoryListView.SelectedItems
                .Cast<object>()
                .Select(item => GetCategoryFromItem(item))
                .Where(cat => cat != null)
                .Cast<Category>()
                .ToList();

            ViewModel.ExtractSelectedCategoriesCommand.Execute(selectedCategories);
        }

        // Für das Hinzufügen einer neuen Kategorie
        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryDialog { XamlRoot = this.XamlRoot };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
            {
                if (NeoCardium.Database.DatabaseHelper.CategoryExists(dialog.EnteredCategoryName))
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Diese Kategorie existiert bereits.", null, this.XamlRoot);
                    return;
                }
                bool success = NeoCardium.Database.DatabaseHelper.AddCategory(dialog.EnteredCategoryName);
                if (!success)
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Kategorie konnte nicht gespeichert werden.", null, this.XamlRoot);
                }
                else
                {
                    await ViewModel.LoadCategoriesAsync();
                }
            }
        }

        // Single-Menu "Bearbeiten" – navigiert zur FlashcardsPage (wie beim normalen Klick)
        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1)
            {
                var cat = GetCategoryFromItem(CategoryListView.SelectedItems[0]);
                if (cat != null)
                {
                    Frame.Navigate(typeof(FlashcardsPage), cat.Id);
                }
            }
        }
    }
}
