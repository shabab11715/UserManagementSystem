using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Task4App.Services
{
    public class EmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailSender(IConfiguration config)
        {
            _apiKey = config["SendGrid:ApiKey"];
            _fromEmail = config["SendGrid:FromEmail"];
            _fromName = config["SendGrid:FromName"];
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var client = new SendGridClient(_apiKey);

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: null,
                htmlContent: htmlBody
            );

            await client.SendEmailAsync(msg);
        }
    }
}
