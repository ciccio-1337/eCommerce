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

        public async Task SendMailAsync(string from, string to, string subject, string body, bool isBodyHtml = false)
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
            var enableSslText = _configuration["MailSettings:Smtp:Network:EnableSsl"] ?? bool.TrueString;
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

            // STARTTLS is enabled unless explicitly disabled. Plaintext SMTP must
            // never be the accidental default.
            if (!bool.TryParse(enableSslText, out var enableSsl))
            {
                enableSsl = true;
            }

            using var message = new MailMessage();

            message.From = new MailAddress(from);

            message.To.Add(to);
            
            message.Subject = subject ?? string.Empty;
            message.Body = body ?? string.Empty;
            // Plain text by default: storefront bodies are StringBuilder-built by callers
            // such as OrderService.SubmitAsync, and the previous heuristic that
            // auto-detected HTML by searching for '<' and '>' would fire on product names
            // like 'Size < 3XL'. Callers that intentionally send HTML opt in explicitly.
            message.IsBodyHtml = isBodyHtml;

            using var smtp = new SmtpClient(host, port);

            smtp.UseDefaultCredentials = useDefaultCredentials;
            smtp.Credentials = new NetworkCredential(userName, password);
            smtp.EnableSsl = enableSsl;

            try
            {
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send an email with subject '{Subject}'.", subject);

                throw;
            }
        }
    }
}