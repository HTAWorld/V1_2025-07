using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace V1_2025_07.Services
{
    public class EmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _config.GetSection("Smtp");

            using (var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"])))
            {
                client.Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]);
                client.EnableSsl = bool.Parse(smtpSection["EnableSsl"]);

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpSection["Sender"]);
                    mail.To.Add(to);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    try
                    {
                        await client.SendMailAsync(mail);
                    }
                    catch (SmtpException ex)
                    {
                        // TODO: Log or rethrow
                        throw new Exception("Failed to send email: " + ex.Message, ex);
                    }
                }
            }
        }
    }
}
