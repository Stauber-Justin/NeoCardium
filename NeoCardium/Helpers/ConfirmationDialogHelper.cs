using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoCardium.Helpers
{
    public static class ConfirmationDialogHelper
    {
        /// <summary>
        /// Shows a delete confirmation dialog for the given items.
        /// </summary>
        /// <typeparam name="T">The type of item to delete.</typeparam>
        /// <param name="items">The collection of items to delete.</param>
        /// <param name="singularName">The singular name (e.g. "die Karteikarte").</param>
        /// <param name="pluralName">The plural name (e.g. "Karteikarten").</param>
        /// <param name="getDisplayName">A function to extract a display string from an item.</param>
        /// <param name="xamlRoot">The XamlRoot to show the dialog.</param>
        /// <returns>The ContentDialogResult of the dialog.</returns>
        public static async Task<ContentDialogResult> ShowDeleteConfirmationDialogAsync<T>(
            IEnumerable<T> items,
            string singularName,
            string pluralName,
            System.Func<T, string> getDisplayName,
            XamlRoot xamlRoot)
        {
            var list = items.ToList();
            if (!list.Any())
                return ContentDialogResult.None;

            string message = list.Count == 1
                ? $"Möchtest du {singularName} '{getDisplayName(list[0])}' wirklich löschen?"
                : $"Möchtest du die {list.Count} ausgewählten {pluralName} wirklich löschen?";

            var dialog = new ContentDialog
            {
                Title = $"{pluralName} löschen",
                Content = message,
                PrimaryButtonText = "Löschen",
                CloseButtonText = "Abbrechen",
                XamlRoot = xamlRoot
            };

            return await dialog.ShowAsync();
        }
    }
}