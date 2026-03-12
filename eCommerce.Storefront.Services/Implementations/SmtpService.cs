using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace eCommerce.Storefront.Services.Implementations
{
    public class SmtpService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendMailAsync(string from, string to, string subject, string body)
        {
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from);
                
                message.To.Add(to);
                
                message.Subject = subject;
                message.Body = body;
                
                using (var smtp = new SmtpClient(_configuration["MailSettingsSmtpNetworkHost"], int.Parse(_configuration["MailSettingsSmtpNetworkPort"])))
                {
                    smtp.UseDefaultCredentials = bool.Parse(_configuration["MailSettingsSmtpNetworkDefaultCredentials"]);
                    smtp.Credentials = new NetworkCredential(_configuration["MailSettingsSmtpNetworkUserName"], _configuration["MailSettingsSmtpNetworkPassword"]);
                    
                    return smtp.SendMailAsync(message);
                }      
            }
        }
    }
}