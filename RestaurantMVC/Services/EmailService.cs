using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RestaurantMVC.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string to, string subject, string htmlBody, string? plainText = null)
        {
            try
            {
                var smtp = _config.GetSection("Smtp");
                var enabled = smtp.GetValue<bool>("Enabled");
                var host = smtp["Host"] ?? string.Empty;
                var port = smtp.GetValue<int?>("Port") ?? 587;
                var useSsl = smtp.GetValue<bool?>("UseSsl") ?? true;
                var username = smtp["Username"];
                var password = smtp["Password"];
                var fromAddress = smtp["FromAddress"] ?? username ?? "no-reply@example.com";
                var fromName = smtp["FromName"] ?? "Restaurantly";

                if (!enabled || string.IsNullOrWhiteSpace(host))
                {
                    _logger.LogInformation("SMTP disabled or host missing; skip sending email to {To}", to);
                    return false;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(fromAddress, fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(plainText))
                {
                    var alt = AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain");
                    message.AlternateViews.Add(alt);
                }

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = useSsl
                };

                if (!string.IsNullOrWhiteSpace(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} subject {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} subject {Subject}", to, subject);
                return false;
            }
        }
    }
}