using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;

namespace ECommerce.Core.Services;

/// <summary>Mock sender — persists messages to the JSON store so the admin Inbox can display them.</summary>
public class EmailService(IRepository<EmailMessage> mailbox) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, EmailType type, CancellationToken ct = default)
    {
        await mailbox.AddAsync(new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            Type = type,
            SentAt = DateTime.UtcNow
        }, ct);
    }
}
