using System.ComponentModel;

namespace NeoCardium.Models
{
    public class Flashcard : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id { get; set; } // Primärschlüssel
        public int CategoryId { get; set; } // Fremdschlüssel zur Kategorie
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";

        private int _correctCount;
        public int CorrectCount
        {
            get => _correctCount;
            set { _correctCount = value; OnPropertyChanged(nameof(CorrectCount)); }
        }

        private int _incorrectCount;
        public int IncorrectCount
        {
            get => _incorrectCount;
            set { _incorrectCount = value; OnPropertyChanged(nameof(IncorrectCount)); }
        }

        public void UpdateCorrectCount()
        {
            CorrectCount++;
        }

        public void UpdateIncorrectCount()
        {
            IncorrectCount++;
        }
    }
}