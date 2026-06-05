namespace Reminder
{
    public static class Message_box
    {
        private static Page? current_page => Application.Current?.Windows[0].Page;


        public static async void Ok(object title, object? text = null, string name = "OK")
        {
            if (current_page is not Page current)
                return;

            if (text is null)
                await current.DisplayAlertAsync("Message", title.ToString(), name);

            else await current.DisplayAlertAsync(title.ToString(), text.ToString(), name);
        }


        public static void Error(object title, object text, string name = "OK")
            => Ok(title + " error", $"Error: {text}", name);

        public static void Show(params object[] texts)
        {
            IEnumerable<string> lst = texts.Select(o => o.ToString()).Where(str => !string.IsNullOrEmpty(str))!;

            if (texts.Length > 0)
                CommunityToolkit.Maui.Alerts.Toast.Make(string.Join(", ", lst)).Show();
        }


        public static async Task<bool> Yes_Not(object title, object text,
            string yes_button_text = "Yes", string no_button_text = "No")
            => current_page is Page current &&
            await current.DisplayAlertAsync(title.ToString(), text.ToString(), yes_button_text, no_button_text);
    }
}
