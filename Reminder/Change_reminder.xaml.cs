using Remind_type = Reminder.Remind.Remind_type;

namespace Reminder;

public partial class Change_reminder : ContentPage
{
    private readonly Remind current;
    private readonly int current_id;
    private readonly bool is_new = false;

    public Change_reminder(Remind? reminder, int new_id)
    {
        InitializeComponent();

        if (reminder is null)
        {
            reminder = Remind.Now;
            is_new = true;
        }

        current = reminder;
        current_id = new_id;

        SetActions();

        PopulateTimePickers();
        InitializeUI();
    }

    private void SetActions()
    {
        IntervalCheck.IsChecked = false;
        StopCheck.IsChecked = false;
        SecChangeBox.IsChecked = true;
        StopCount.IsEnabled = false;

        SecChangeBox.CheckedChanged += (_, args) =>
        {
            NextTimeSecondPicker.IsVisible = args.Value;
            StopTimeSecondPicker.IsVisible = args.Value;

            if (!args.Value)
            {
                NextTimeSecondPicker.SelectedIndex = 0;
                StopTimeSecondPicker.SelectedIndex = 0;
            }
        };

        IntervalCheck.CheckedChanged += (_, args) =>
        {
            if (!args.Value)
            {
                StopCheck.IsChecked = false;
                StopCheck.IsEnabled = false;
            }
            else StopCheck.IsEnabled = true;
        };

        StopSwitch.Toggled += (_, args) =>
        {
            StopCount.IsEnabled = args.Value;
            StopDate.IsEnabled = !args.Value;
        };

    }

    private void PopulateTimePickers()
    {
        for (int m = 0; m < 60; m++)
        {
            string text = m.ToString("D2");
            NextTimeSecondPicker.Items.Add(text);
            IntervalTimeSecondPicker.Items.Add(text);
            StopTimeSecondPicker.Items.Add(text);
        }

        NextTimeSecondPicker.SelectedIndex = 0;
        IntervalTimeSecondPicker.SelectedIndex = 0;
        StopTimeSecondPicker.SelectedIndex = 0;
    }

    private void InitializeUI()
    {
        if (!is_new)
        {
            CreateRemindButton.Text = "Update";
            DeleteRemindButton.IsVisible = true;
            Title = "Edit reminder";
        }

        MessageEntry.Text = current.message;

        NextDatePicker.Date = current.next.Date;
        SetTimePickers(NextTimePicker, NextTimeSecondPicker, current.next.TimeOfDay);

        if ((current.type & Remind.WithInterval) != 0)
        {
            IntervalCheck.IsChecked = true;
            DateTime now = DateTime.Now;
            DateTime date = now.Add(current.interval);

            IntervalYearsEntry.Text = (date.Year - now.Year).ToString();
            IntervalMonthsEntry.Text = (date.Month - now.Month).ToString();
            IntervalDaysEntry.Text = (date.Day - now.Day).ToString();

            SetTimePickers(IntervalTimePicker, IntervalTimeSecondPicker, current.interval);

            if (current.type == Remind_type.Repeat_with_stop_date)
            {
                StopCheck.IsChecked = true;

                StopDatePicker.Date = current.stop.Date;
                SetTimePickers(StopTimePicker, StopTimeSecondPicker, current.stop.TimeOfDay);
            }
            else if (current.type == Remind_type.Repeat_with_stop_count)
            {
                StopCheck.IsChecked = true;
                StopSwitch.IsToggled = true;
                StopCount.IsEnabled = true;
                StopDate.IsEnabled = false;

                StopCountEntry.Text = current.StopAfter.ToString();
            }
        }
    }

    private static DateTimeOffset GetDateTimeOffset(DatePicker datePicker, TimePicker timePicker, Picker timeSecondPicker)
    {
        DateTime Date = datePicker?.Date ?? System.DateTime.Today;
        TimeSpan Time = GetTimeFromPickers(timePicker, timeSecondPicker)!.Value;

        DateTime DateTime = new(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds);
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime);
        return new(DateTime, offset);
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        try
        {
            string? message = MessageEntry?.Text?.Trim();
            if (string.IsNullOrEmpty(message))
            {
                Message_box.Show("Message cannot be empty.");
                return;
            }

            DateTimeOffset next = GetDateTimeOffset(NextDatePicker, NextTimePicker, NextTimeSecondPicker);

            TimeSpan interval = default;
            if (IntervalCheck?.IsChecked == true)
            {
                if (int.TryParse(IntervalDaysEntry?.Text, out int days) && days < 0)
                {
                    Message_box.Show("Days in interval must be positive");
                    return;
                }
                if (int.TryParse(IntervalMonthsEntry?.Text, out int months) && days < 0)
                {
                    Message_box.Show("Days in interval must be positive");
                    return;
                }
                if (int.TryParse(IntervalYearsEntry?.Text, out int years) && years < 0)
                {
                    Message_box.Show("Years in interval must be positive");
                    return;
                }

                interval = DateTime.Now.AddYears(years).AddMonths(months).AddDays(days) - DateTime.Now +
                    GetTimeFromPickers(IntervalTimePicker, IntervalTimeSecondPicker)!.Value;

                if (interval <= TimeSpan.Zero)
                {
                    Message_box.Show("Interval must be greater than zero.");
                    return;
                }
            }

            DateTimeOffset stop = default;
            int count = -1;
            if (StopCheck?.IsChecked == true)
            {
                if (IntervalCheck?.IsChecked != true)
                    return;

                if (!StopSwitch.IsToggled)
                {
                    stop = GetDateTimeOffset(StopDatePicker, StopTimePicker, StopTimeSecondPicker);
                    if (stop <= next)
                    {
                        Message_box.Show("Stop date must be greater than next date.");
                        return;
                    }
                }
                else if(int.TryParse(StopCountEntry?.Text, out count) && count < 0)
                {
                    Message_box.Show("Stop count must be positive");
                    return;
                }
            }


            Remind remind = Remind.FromDate(current_id, message, next, interval, stop, count);

            if (remind.next <= DateTimeOffset.Now)
            {
                Message_box.Show("Next date must be in the future.");
                return;
            }
            if (!is_new)
                Notification_helper.Cancel(current.id);

            Notification_helper.CreateNew(remind);

            await Navigation.PopAsync();
        }
        catch (OverflowException)
        {
            Message_box.Error("Overflow", "One of the values is too large.");
        }
        catch (Exception ex)
        {
            Message_box.Error(nameof(OnCreateClicked), ex.Message);
        }
    }

    private static void SetTimePickers(TimePicker timePicker, Picker seconds, TimeSpan time)
    {
        timePicker.Time = time;
        seconds.SelectedIndex = Math.Clamp(time.Seconds, 0, 59);
    }

    private static TimeSpan? GetTimeFromPickers(TimePicker timePicker, Picker second)
        => timePicker.Time + TimeSpan.FromSeconds(second.SelectedIndex >= 0 ? second.SelectedIndex : 0);

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (!await Message_box.Yes_Not("Edit reminder", "Are you sure you want to delete this reminder?"))
            return;

        Notification_helper.Cancel(current!.id);
        await Navigation.PopAsync();
    }
}