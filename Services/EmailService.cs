using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken
    )
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

        email.To.Add(MailboxAddress.Parse(toEmail));

        email.Subject = subject;

        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);

        await smtp.SendAsync(email, cancellationToken);

        await smtp.DisconnectAsync(true);
    }
}
