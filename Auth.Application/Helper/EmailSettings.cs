namespace Auth.Application.Helper
{
    // Config loaded from the "EmailSettings" section (in user secrets, like JWT)
    public class EmailSettings
    {
        public string FromEmail { get; set; } = string.Empty;   // the Gmail address that SENDS
        public string DisplayName { get; set; } = string.Empty;  // shown as the sender name
        public string Password { get; set; } = string.Empty;     // Gmail "App Password" (16 chars, NOT your login password)
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string ConfirmUrlBase { get; set; } = "http://localhost:5032/api/auth/confirm-email";
    }
}
