using System.Net;
using System.Net.Mail;

namespace question_answer.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var host = _config["Email:Host"] ?? Environment.GetEnvironmentVariable("EMAIL_HOST") ?? string.Empty;
        var portStr = _config["Email:Port"] ?? Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "587";
        var user = _config["Email:User"] ?? Environment.GetEnvironmentVariable("EMAIL_USER") ?? string.Empty;
        var pass = _config["Email:Pass"] ?? Environment.GetEnvironmentVariable("EMAIL_PASS") ?? string.Empty;
        var from = _config["Email:From"] ?? Environment.GetEnvironmentVariable("EMAIL_FROM") ?? user;

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            Console.WriteLine("[EmailService] Missing SMTP configuration. Email not sent.");
            return;
        }

        if (!int.TryParse(portStr, out int port))
        {
            port = 587;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
