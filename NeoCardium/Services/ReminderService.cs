using System;
using System.Linq;
using Windows.UI.Notifications;
using CommunityToolkit.WinUI.Notifications;

namespace NeoCardium.Services
{
    public static class ReminderService
    {
        private const string ToastTag = "DailyReminder";

        public static void ScheduleDailyReminder(TimeSpan time)
        {
            var notifier = ToastNotificationManager.CreateToastNotifier();
            foreach (var scheduled in notifier.GetScheduledToastNotifications().Where(t => t.Tag == ToastTag).ToList())
            {
                notifier.RemoveFromSchedule(scheduled);
            }

            var next = DateTime.Today.Add(time);
            if (next <= DateTime.Now)
            {
                next = next.AddDays(1);
            }

            var xml = new ToastContentBuilder()
                .AddText("NeoCardium")
                .AddText("Zeit zum Lernen!")
                .GetXml();

            var toast = new ScheduledToastNotification(xml, next) { Tag = ToastTag };
            notifier.AddToSchedule(toast);
        }

        public static void CancelReminder()
        {
            var notifier = ToastNotificationManager.CreateToastNotifier();
            foreach (var scheduled in notifier.GetScheduledToastNotifications().Where(t => t.Tag == ToastTag).ToList())
            {
                notifier.RemoveFromSchedule(scheduled);
            }
        }
    }
}
