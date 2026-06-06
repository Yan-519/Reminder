using Plugin.LocalNotification.Core.Models;
using System.Text;

namespace Reminder
{
    public class Remind : IComparable<Remind>, IEquatable<Remind>
    {
        [Flags]
        public enum Remind_type
        {
            Once = 0,
            Repeat_without_stop = 1 << 0,
            Repeat_with_stop_date = 1 << 1,
            Repeat_with_stop_count = 1 << 2
        }

        public static readonly Remind_type WithInterval =
            Remind_type.Repeat_without_stop |
            Remind_type.Repeat_with_stop_date |
            Remind_type.Repeat_with_stop_count;

        public static readonly Remind Now = new(-1, string.Empty, DateTimeOffset.Now);

        public Remind_type type { init; get; }

        public int id { private set; get; }

        public string message { private set; get; }

        public DateTimeOffset next { private set; get; }
        public TimeSpan interval { private set; get; } = TimeSpan.Zero;
        public DateTimeOffset stop { private set; get; } = DateTimeOffset.Now;

        public int StopAfter { private set; get; } = 0;

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
            type = Remind_type.Repeat_with_stop_date;

            this.id = id;
            this.message = message;
            this.next = next;
            this.interval = interval;
            this.stop = stop;

            Update_next();
        }

        public Remind(int id, string message, DateTimeOffset next, TimeSpan interval, int count)
        {
            type = Remind_type.Repeat_with_stop_count;

            this.id = id;
            this.message = message;
            this.next = next;
            this.interval = interval;
            StopAfter = count;

            Update_next();
        }


        private void Update_next()
        {
            if (type == Remind_type.Once)
                return;

            else if (type == Remind_type.Repeat_with_stop_count)
                for (; next < DateTimeOffset.Now && 0 < StopAfter; StopAfter--)
                    next = next.Add(interval);

            else while (next < DateTimeOffset.Now)
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
            int Id = notification.NotificationId;
            string Message = notification.Description;
            DateTimeOffset? next = notification.Schedule.NotifyTime;
            TimeSpan? interval = notification.Schedule.NotifyRepeatInterval;
            DateTimeOffset? stop_date = notification.Schedule.NotifyAutoCancelTime;

            string Subtitle = notification.Subtitle;

            if (next is null)
            {
                remind = Now;
                return false;
            }

            else if (stop_date is not null)
                remind = new Remind(Id, Message, next.Value, interval!.Value, stop_date.Value);
            else if (!string.IsNullOrEmpty(Subtitle))
            {
                StringBuilder builder = new();
                foreach (char ch in Subtitle)
                    if (char.IsDigit(ch))
                        builder.Append(ch);

                remind = new Remind(Id, Message, next.Value, interval!.Value, int.Parse(builder.ToString()));
            }
            else if (interval is not null)
                remind = new Remind(Id, Message, next.Value, interval.Value);
            else
                remind = new Remind(Id, Message, next.Value);

            return true;
        }

        public NotificationRequest ToNotificationRequest()
        {
            NotificationRequestSchedule schedule = new()
            {
                NotifyTime = next,
                Android =
                {
                    ScheduleMode = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidScheduleMode.ExactAllowWhileIdle
                }
            };

            if ((type & WithInterval) != 0)
            {
                schedule.RepeatType = NotificationRepeat.TimeInterval;
                schedule.NotifyRepeatInterval = interval;

                if (type == Remind_type.Repeat_with_stop_date)
                    schedule.NotifyAutoCancelTime = stop;
            }

            NotificationRequest request = new()
            {
                NotificationId = id,
                Title = AppInfo.Name,
                Description = message,
                Subtitle = StopAfter == 0 ? string.Empty : $"Stops after {StopAfter} times",
                Schedule = schedule
            };
            return request;
        }

        public static Remind FromDate(int Id, string Message, DateTimeOffset next, TimeSpan interval, DateTimeOffset stop, int Count)
        {
            Remind remind;
            if (stop != default)
                remind = new Remind(Id, Message, next, interval, stop);
            else if (Count != -1)
                remind = new Remind(Id, Message, next, interval, Count);
            else if (interval != default)
                remind = new Remind(Id, Message, next, interval);
            else
                remind = new Remind(Id, Message, next);

            return remind;
        }

        public bool IsOld() => type switch
        {
            Remind_type.Once => next < DateTimeOffset.Now,
            Remind_type.Repeat_with_stop_date => stop < DateTimeOffset.Now,
            Remind_type.Repeat_with_stop_count => StopAfter <= 0,
            _ => false
        };

    }
}
