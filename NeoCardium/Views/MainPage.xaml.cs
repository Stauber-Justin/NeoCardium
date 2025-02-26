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
using NeoCardium.Models;
using NeoCardium.Database;
using System.Runtime.Versioning;

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
            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            foreach (var category in DatabaseHelper.GetCategories())
            {
                Categories.Add(category);
            }

            CategoryListView.ItemsSource = Categories;
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
            var categoryDialog = new CategoryDialog();
            categoryDialog.XamlRoot = this.XamlRoot; // Setzt den Dialog ins UI-Root

            var dialogResult = await categoryDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(categoryDialog.EnteredCategoryName))
            {
                DatabaseHelper.AddCategory(categoryDialog.EnteredCategoryName);
                LoadCategories(); // Aktualisiert die Liste
            }
        }
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuFlyoutItem)?.DataContext is Category selectedCategory)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Kategorie löschen",
                    Content = $"Möchtest du die Kategorie '{selectedCategory.CategoryName}' wirklich löschen?",
                    PrimaryButtonText = "Löschen",
                    CloseButtonText = "Abbrechen",
                    XamlRoot = this.XamlRoot // Setzt den Dialog ins UI-Root
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    DatabaseHelper.DeleteCategory(selectedCategory.Id);
                    LoadCategories();
                }
            }
        }


        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuFlyoutItem)?.DataContext is Category selectedCategory)
            {
                var dialog = new CategoryDialog();
                dialog.EnteredCategoryName = selectedCategory.CategoryName; // Setzt den alten Namen in das Textfeld
                dialog.XamlRoot = this.XamlRoot; // Setzt den Dialog ins UI-Root

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.EnteredCategoryName))
                {
                    DatabaseHelper.UpdateCategory(selectedCategory.Id, dialog.EnteredCategoryName);
                    LoadCategories();
                }
            }
        }

    }
}
