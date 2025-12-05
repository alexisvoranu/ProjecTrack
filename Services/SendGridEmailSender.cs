using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Licenta3.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Missing SENDGRID_API_KEY!");

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromEmail, "ProjecTrack");
            var to = new EmailAddress(email);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

            await client.SendEmailAsync(msg);
        }
    }
}
