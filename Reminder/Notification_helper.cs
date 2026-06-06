using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace Reminder
{
    public static class Notification_helper
    {
        public static async Task<bool> is_have_notification_permission()
            => (await Permissions.CheckStatusAsync<Permissions.PostNotifications>()) == PermissionStatus.Granted;

        public static async Task<bool> ask_permission()
        {
            if (await is_have_notification_permission())
                return true;

            await Permissions.RequestAsync<Permissions.PostNotifications>();

            return await is_have_notification_permission();
        }

        public static async void CreateNew(Remind remind)
        {
            if (!await is_have_notification_permission() || string.IsNullOrEmpty(remind.message))
                return;

            await LocalNotificationCenter.Current.Show(remind.ToNotificationRequest());
        }

        public static async void ManageScheduledNotifications()
        {
            var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();

            foreach (NotificationRequest notification in pending)
            {
                if (Remind.TryParse(notification, out Remind remind) && remind.IsOld())
                {
                    LocalNotificationCenter.Current.Cancel(notification.NotificationId);
                }
            }
        }

        public static void Cancel(int id)
            => LocalNotificationCenter.Current.Cancel(id);

        public static async Task<List<Remind>> GetAll()
        {
            ManageScheduledNotifications();
            var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();

            List<Remind> reminds = [];

            foreach (NotificationRequest notification in pending)
                if (Remind.TryParse(notification, out Remind remind))
                    reminds.Add(remind);

            return reminds;
        }

    }
}
