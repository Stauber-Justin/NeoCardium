using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeoCardium.Models
{
    public class GamificationStats : INotifyPropertyChanged
    {
        private int _points;
        public int Points
        {
            get => _points;
            set { _points = value; OnPropertyChanged(); }
        }

        private int _streak;
        public int Streak
        {
            get => _streak;
            set { _streak = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _badges = new();
        public ObservableCollection<string> Badges
        {
            get => _badges;
            set { _badges = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
