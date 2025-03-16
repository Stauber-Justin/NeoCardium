using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeoCardium.Models
{
    public class FlashcardAnswer : INotifyPropertyChanged
    {
        private int _answerId;
        public int AnswerId
        {
            get => _answerId;
            set { _answerId = value; OnPropertyChanged(); }
        }

        private int _index;
        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        private int _flashcardId;
        public int FlashcardId
        {
            get => _flashcardId;
            set { _flashcardId = value; OnPropertyChanged(); }
        }

        private string _answerText = "";
        public string AnswerText
        {
            get => _answerText;
            set { _answerText = value; OnPropertyChanged(); }
        }

        private bool _isCorrect;
        public bool IsCorrect
        {
            get => _isCorrect;
            set
            {
                if (_isCorrect != value)
                {
                    _isCorrect = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
