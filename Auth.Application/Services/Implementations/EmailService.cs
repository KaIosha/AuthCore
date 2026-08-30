using System;
using System.Threading.Tasks;
using Auth.Application.Helper;
using Auth.Application.Services.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Application.Services.Implementations
{

    // Sends emails through Gmail's SMTP server (MailKit).
    // Gmail requires an "App Password" (not your normal Gmail password).
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            // Step 1: Build the email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

            // Step 2: Connect to Gmail's SMTP server
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);

            // Step 3: Authenticate with the App Password
            await client.AuthenticateAsync(_settings.FromEmail, _settings.Password);

            // Step 4: Send and disconnect
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
