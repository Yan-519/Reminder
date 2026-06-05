using Plugin.LocalNotification.Core.Models;

namespace Reminder
{
    public class Remind : IComparable<Remind>, IEquatable<Remind>
    {
        public enum Remind_type
        {
            Once,
            Repeat_without_stop,
            Repeat_with_stop,
        }

        public static readonly Remind Now = new(-1, string.Empty, DateTimeOffset.Now);

        public Remind_type type { init; get; }

        public int id { private set; get; }

        public string message { private set; get; }

        public DateTimeOffset next { private set; get; }
        public TimeSpan interval { private set; get; } = TimeSpan.Zero;
        public DateTimeOffset stop { private set; get; } = DateTimeOffset.Now;

        public string view_date => message + " \n" + (next.Second != 0 ? next.ToString("dd/MM/yyyy-HH:mm:ss") : next.ToString("dd/MM/yyyy-HH:mm"));

        public DateTime next_date => next.DateTime;

        public long interval_to_sec => (long)interval.TotalSeconds;

        public Remind(int id, string message, DateTimeOffset next)
        {
            type = Remind_type.Once;

            this.id = id;
            this.message = message;
            this.next = next;
        }

        public Remind(int id, string message, DateTimeOffset next, TimeSpan interval)
        {
            type = Remind_type.Repeat_without_stop;

            this.id = id;
            this.message = message;
            this.next = next;
            this.interval = interval;

            Update_next();
        }

        public Remind(int id, string message, DateTimeOffset next, TimeSpan interval, DateTimeOffset stop)
        {
            type = Remind_type.Repeat_with_stop;

            this.id = id;
            this.message = message;
            this.next = next;
            this.interval = interval;
            this.stop = stop;

            Update_next();
        }

        private void Update_next()
        {
            if (type == Remind_type.Once)
                return;

            while (next < DateTimeOffset.Now)
                next = next.Add(interval);
        }

        public int CompareTo(Remind? other)
        {
            if (other is null)
                return 1;
            return next.CompareTo(other.next);
        }

        public bool Equals(Remind? other)
        {
            if (other is null)
                return false;
            return id == other.id;
        }

        public override bool Equals(object? obj)
            => obj is Remind other && Equals(other);

        public override int GetHashCode() => id;

        public override string ToString()
        {
            return view_date + "\n" +
                interval.TotalSeconds + "\n" +
                stop.ToString("dd,MM,yyyy-HH:mm:ss");
        }

        public static bool TryParse(NotificationRequest notification, out Remind remind)
        {
            int id = notification.NotificationId;
            string message = notification.Description;
            DateTimeOffset? next = notification.Schedule.NotifyTime;
            TimeSpan? interval = notification.Schedule.NotifyRepeatInterval;
            DateTimeOffset? stop_date = notification.Schedule.NotifyAutoCancelTime;

            if (next is null)
            {
                remind = Now;
                return false;
            }

            else if (stop_date is not null)
                remind = new Remind(id, message, next.Value, interval!.Value, stop_date.Value);
            else if (interval is not null)
                remind = new Remind(id, message, next.Value, interval.Value);
            else
                remind = new Remind(id, message, next.Value);

            return true;
        }

        public NotificationRequest ToNotificationRequest()
        {
            NotificationRequestSchedule schedule = new()
            {
                NotifyTime = this.next,
                Android =
                {
                    ScheduleMode = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleMode.ExactAllowWhileIdle
                }
            };

            if (this.type == Remind_type.Repeat_without_stop || this.type == Remind_type.Repeat_with_stop)
            {
                schedule.RepeatType = NotificationRepeat.TimeInterval;
                schedule.NotifyRepeatInterval = this.interval;

                if (this.type == Remind_type.Repeat_with_stop)
                    schedule.NotifyAutoCancelTime = this.stop;
            }

            NotificationRequest request = new()
            {
                NotificationId = this.id,
                Title = AppInfo.Name,
                Description = this.message,
                Schedule = schedule
            };
            return request;
        }

        public static Remind FromDate(int Id, string Message, DateTimeOffset next, TimeSpan interval, DateTimeOffset stop)
        {
            Remind remind;
            if (interval == default)
                remind = new Remind(Id, Message, next);
            else if (stop == default)
                remind = new Remind(Id, Message, next, interval);
            else
                remind = new Remind(Id, Message, next, interval, stop);

            return remind;
        }
    }
}
