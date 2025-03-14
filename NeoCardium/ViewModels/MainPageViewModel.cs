using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NeoCardium.Models;
using NeoCardium.Database;
using NeoCardium.Helpers;
using System.Collections.Generic;

namespace NeoCardium.ViewModels
{
    public class ObservableObject : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;
            storage = value;
            if (propertyName != null)
                OnPropertyChanged(propertyName);
            return true;
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainPageViewModel : ObservableObject
    {
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        public ICommand DeleteCategoryCommand { get; }
        public ICommand DeleteSelectedCategoriesCommand { get; }
        public ICommand ExtractSelectedCategoriesCommand { get; }

        public MainPageViewModel()
        {
            DeleteCategoryCommand = new RelayCommand(async (param) => await DeleteCategoryAsync(param as Category));
            DeleteSelectedCategoriesCommand = new RelayCommand(async (param) => await DeleteSelectedCategoriesAsync(param as IEnumerable<Category>));
            ExtractSelectedCategoriesCommand = new RelayCommand((param) => ExtractSelectedCategories(param as IEnumerable<Category>));
        }

        public async Task LoadCategoriesAsync()
        {
            var cats = DatabaseHelper.GetCategories();
            Categories.Clear();
            foreach (var cat in cats)
            {
                Categories.Add(cat);
            }
            await Task.CompletedTask;
        }

        private async Task DeleteCategoryAsync(Category? category)
        {
            if (category == null)
                return;
            DatabaseHelper.DeleteCategory(category.Id);
            await LoadCategoriesAsync();
        }

        private async Task DeleteSelectedCategoriesAsync(IEnumerable<Category>? selectedCategories)
        {
            if (selectedCategories == null || !selectedCategories.Any())
                return;

            foreach (var cat in selectedCategories.ToList())
            {
                DatabaseHelper.DeleteCategory(cat.Id);
            }
            await LoadCategoriesAsync();
        }

        private void ExtractSelectedCategories(IEnumerable<Category>? selectedCategories)
        {
            // Placeholder – hier kann die Extraktionslogik implementiert werden.
        }
    }
}
