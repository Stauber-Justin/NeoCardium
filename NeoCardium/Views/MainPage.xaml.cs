using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NeoCardium.Models;
using NeoCardium.ViewModels;
using NeoCardium.Helpers;
using NeoCardium.Database;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace NeoCardium.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; } = new MainPageViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;

            // Create a new Database instance.
            var database = new Database.Database();
            database.Initialize();

            DebugUtility.InitializeDebugData();   // Only inserts test data when debugging
            Loaded += MainPage_Loaded; // Load Categories Async

        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
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
                // If this item is not already selected, select only it.
                if (!CategoryListView.SelectedItems.Cast<object>()
                        .Any(item => GetCategoryFromItem(item)?.Id == cat.Id))
                {
                    CategoryListView.SelectedItems.Clear();
                    CategoryListView.SelectedItems.Add(cat);
                }
            }

            int count = CategoryListView.SelectedItems.Count;
            if (count == 0) return;

            MenuFlyout flyout = (count == 1) ?
                (MenuFlyout)Resources["SingleCategoryContextFlyout"] :
                (MenuFlyout)Resources["MultiCategoryContextFlyout"];

            flyout.ShowAt(CategoryListView, new FlyoutShowOptions { Position = e.GetPosition(CategoryListView) });
            e.Handled = true;
        }

        // Helper to extract the Category from an item (or its container)
        private Category? GetCategoryFromItem(object? item)
        {
            if (item is Category cat)
                return cat;
            if (item is ListViewItem lvi && lvi.DataContext is Category cat2)
                return cat2;
            return null;
        }

        // Single delete using shared confirmation dialog
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1)
            {
                var cat = GetCategoryFromItem(CategoryListView.SelectedItems[0]);
                if (cat != null)
                {
                    var list = new List<Category> { cat };
                    var result = await ConfirmationDialogHelper.ShowDeleteConfirmationDialogAsync(
                        list, "die Kategorie", "Kategorien", c => c.CategoryName, this.XamlRoot);
                    if (result == ContentDialogResult.Primary)
                    {
                        ViewModel.DeleteCategoryCommand.Execute(cat);
                    }
                }
            }
        }

        // Multi delete using shared confirmation dialog
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

            var result = await ConfirmationDialogHelper.ShowDeleteConfirmationDialogAsync(
                selectedCategories, "die Kategorie", "Kategorien", c => c.CategoryName, this.XamlRoot);
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteSelectedCategoriesCommand.Execute(selectedCategories);
            }
        }

        // Multi-menu "Auswahl Exportieren" handler.
        private void ExportSelectedCategories_Click(object sender, RoutedEventArgs e)
        {
            var selectedCategories = CategoryListView.SelectedItems
                .Cast<object>()
                .Select(item => GetCategoryFromItem(item))
                .Where(cat => cat != null)
                .Cast<Category>()
                .ToList();

            ViewModel.ExportCategoriesCommand.Execute(selectedCategories);
        }

        // Single-menu "Exportieren" handler.
        private void ExportCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItems.Count == 1)
            {
                var cat = GetCategoryFromItem(CategoryListView.SelectedItems[0]);
                if (cat != null)
                {
                    var list = new List<Category> { cat };
                    ViewModel.ExportCategoriesCommand.Execute(list);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportCategoriesCommand.Execute(null);
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryDialog { XamlRoot = this.XamlRoot };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
            {
                if (DatabaseHelper.Instance.CategoryExists(dialog.EnteredCategoryName))
                {
                    await ExceptionHelper.ShowErrorDialogAsync("Diese Kategorie existiert bereits.", null, this.XamlRoot);
                    return;
                }
                bool success = DatabaseHelper.Instance.AddCategory(dialog.EnteredCategoryName);
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

        // Single-menu "Bearbeiten" navigates like an item click.
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
