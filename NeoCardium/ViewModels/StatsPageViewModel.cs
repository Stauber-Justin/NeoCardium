using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoCardium.Database;
using NeoCardium.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace NeoCardium.ViewModels
{
    public partial class StatsPageViewModel : ObservableObject
    {
        public ObservableCollection<DailyStat> DailyStats { get; } = new();

        [RelayCommand]
        private void LoadStats()
        {
            DailyStats.Clear();
            foreach (var stat in DatabaseHelper.Instance.GetDailyStatistics())
            {
                DailyStats.Add(stat);
            }
        }

        [RelayCommand]
        private async Task ExportStatsAsync()
        {
            var csv = DatabaseHelper.Instance.ExportStatisticsCsv();
            FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("CSV", new() { ".csv" });
            savePicker.SuggestedFileName = "statistics.csv";
            var hwnd = WindowNative.GetWindowHandle(App._mainWindow);
            InitializeWithWindow.Initialize(savePicker, hwnd);
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, csv);
            }
        }

        [RelayCommand]
        private void ResetStats()
        {
            if (DatabaseHelper.Instance.ResetStatistics())
            {
                LoadStats();
            }
        }
    }
}
