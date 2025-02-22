using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName)
    {
        var emailSettings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("CCMS", emailSettings["SenderEmail"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };

        // Attach files if provided
        if (attachmentData != null)
        {
            bodyBuilder.Attachments.Add(attachmentName, attachmentData, ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(emailSettings["SMTPServer"], int.Parse(emailSettings["Port"]), MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["SenderPassword"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
