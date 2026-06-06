
namespace Reminder
{
    public partial class MainPage : ContentPage
    {
        public static readonly Random random = new();

        private List<Remind> reminders_collection = [];

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            if (!await Notification_helper.ask_permission())
            {
                Message_box.Show("Without notification permission the app can't launch ");
                Application.Current?.Quit();
            }

            reminders_collection = await Notification_helper.GetAll();

            reminders_collection.Sort();

            reminders_view.ItemsSource = reminders_collection;
        }

        private async void edit_reminder(object sender, EventArgs e)
        {
            if (sender is not Button bt)
            {
                Message_box.Error(nameof(edit_reminder), "sender not button");
                return;
            }

            int id = (int)bt.BindingContext;

            await Navigation.PushAsync(new Change_reminder(reminders_collection.FirstOrDefault(x => x.id == id), id));
        }

        private async void create_new_reminder(object sender, EventArgs e)
        {
            HashSet<int> ids = reminders_collection.Select(x => x.id).ToHashSet();

            int id;
            do
            {
                id = random.Next();
            }
            while (ids.Contains(id));

            await Navigation.PushAsync(new Change_reminder(null, id));
        }

        private void refresh_list(object sender, EventArgs e)
        {
            Notification_helper.ManageScheduledNotifications();
            OnAppearing();
        }
    }
}
