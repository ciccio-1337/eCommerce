using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.Services.Implementations
{
    public class SmtpService(IConfiguration configuration, ILogger<SmtpService> logger) : IEmailService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<SmtpService> _logger = logger;

        public async Task SendMailAsync(string from, string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning("SendMailAsync aborted: 'from' address is empty.");
                
                return;
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("SendMailAsync aborted: 'to' address is empty.");
                
                return;
            }

            var host = _configuration["MailSettings:Smtp:Network:Host"] ?? _configuration["MailSettingsSmtpNetworkHost"] ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("SendMailAsync aborted: SMTP Host is not configured.");
                
                return;
            }

            var portText = _configuration["MailSettings:Smtp:Network:Port"] ?? _configuration["MailSettingsSmtpNetworkPort"] ?? "25";
            var useDefaultText = _configuration["MailSettings:Smtp:Network:DefaultCredentials"] ?? _configuration["MailSettingsSmtpNetworkDefaultCredentials"] ?? bool.FalseString;
            var userName = _configuration["MailSettings:Smtp:Network:UserName"] ?? _configuration["MailSettingsSmtpNetworkUserName"] ?? string.Empty;
            var password = _configuration["MailSettings:Smtp:Network:Password"] ?? _configuration["MailSettingsSmtpNetworkPassword"] ?? string.Empty;

            if (!int.TryParse(portText, out var port))
            {
                port = 25;
            }

            if (!bool.TryParse(useDefaultText, out var useDefaultCredentials))
            {
                useDefaultCredentials = false;
            }

            using var message = new MailMessage();

            message.From = new MailAddress(from);

            message.To.Add(to);
            
            message.Subject = subject ?? string.Empty;
            message.Body = body ?? string.Empty;
            message.IsBodyHtml = body != null && body.Contains('<') && body.Contains('>');

            using var smtp = new SmtpClient(host, port);

            smtp.UseDefaultCredentials = useDefaultCredentials;
            smtp.Credentials = new NetworkCredential(userName, password);

            try
            {
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'.", to, subject);

                throw;
            }
        }
    }
}