using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _smtpHost = configuration["Email:SmtpHost"] ?? throw new ArgumentNullException("Email:SmtpHost");
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUser = configuration["Email:SmtpUser"] ?? throw new ArgumentNullException("Email:SmtpUser");
        _smtpPass = configuration["Email:SmtpPass"] ?? throw new ArgumentNullException("Email:SmtpPass");
        _fromEmail = configuration["Email:FromEmail"] ?? _smtpUser;
        _fromName = configuration["Email:FromName"] ?? "Invoice Service";
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(_fromEmail, _fromName);
        message.To.Add(to);
        message.Subject = subject;
        message.Body = htmlContent;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }
}