using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SparkleMail.Models;

namespace SparkleMail.Services;

public class MailKitEmailService
{
    // Перевірка з'єднання
    public async Task<bool> TestConnectionAsync(AccountSettings settings)
    {
        try
        {
            using (var imap = new ImapClient())
            {
                await imap.ConnectAsync(settings.ImapServer, settings.ImapPort, true);
                await imap.AuthenticateAsync(settings.Email, settings.Password);
                await imap.DisconnectAsync(true);
            }

            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(settings.SmtpServer, settings.SmtpPort, SecureSocketOptions.Auto);
                await smtp.AuthenticateAsync(settings.Email, settings.Password);
                await smtp.DisconnectAsync(true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Шукаємо папку "Надіслані": спочатку за стандартом IMAP, потім за назвою
    private static async Task<IMailFolder?> GetSentFolderAsync(ImapClient imap)
    {
        try
        {
            var sent = imap.GetFolder(SpecialFolder.Sent);
            if (sent != null) return sent;
        }
        catch (NotSupportedException) { }

        var folders = await imap.GetFoldersAsync(imap.PersonalNamespaces[0]);
        string[] names = { "Sent", "Sent Items", "Sent Mail", "Відправлені", "Надіслані", "Отправленные" };
        return folders.FirstOrDefault(f => names.Contains(f.Name, StringComparer.OrdinalIgnoreCase));
    }

    // Список заголовків: вхідні або надіслані (нові зверху)
    public async Task<List<EmailMessageModel>> FetchFolderAsync(AccountSettings settings, bool sent, int count = 20)
    {
        var list = new List<EmailMessageModel>();

        using var imap = new ImapClient();
        await imap.ConnectAsync(settings.ImapServer, settings.ImapPort, true);
        await imap.AuthenticateAsync(settings.Email, settings.Password);

        IMailFolder? folder = sent ? await GetSentFolderAsync(imap) : imap.Inbox;
        if (folder == null)
            throw new InvalidOperationException("Папку «Надіслані» не знайдено на сервері");

        await folder.OpenAsync(FolderAccess.ReadOnly);

        int total = folder.Count;
        if (total == 0)
        {
            await imap.DisconnectAsync(true);
            return list;
        }

        var summaries = await folder.FetchAsync(
            Math.Max(0, total - count), total - 1,
            MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.UniqueId);

        foreach (var s in summaries.Reverse())
        {
            var e = s.Envelope;

            // у вхідних показуємо відправника, у надісланих - одержувача
            var person = sent
                ? e.To.Mailboxes.FirstOrDefault()
                : e.From.Mailboxes.FirstOrDefault();

            string subject = e.Subject ?? "(Без теми)";

            list.Add(new EmailMessageModel
            {
                UniqueId = s.UniqueId.Id,
                Sender = !string.IsNullOrWhiteSpace(person?.Name) ? person!.Name : person?.Address ?? "",
                SenderEmail = person?.Address ?? "",
                Subject = subject,
                Date = e.Date?.LocalDateTime ?? DateTime.Now,
                IsSeen = sent || s.Flags!.Value.HasFlag(MessageFlags.Seen),
                Preview = subject
            });
        }

        await imap.DisconnectAsync(true);
        return list;
    }

    // Лист повністю: повертаємо готовий HTML для WebView
    public async Task<string> GetMessageHtmlAsync(AccountSettings settings, uint uniqueId, bool sent)
    {
        using var imap = new ImapClient();
        await imap.ConnectAsync(settings.ImapServer, settings.ImapPort, true);
        await imap.AuthenticateAsync(settings.Email, settings.Password);

        IMailFolder? folder = sent ? await GetSentFolderAsync(imap) : imap.Inbox;
        if (folder == null)
            throw new InvalidOperationException("Папку «Надіслані» не знайдено на сервері");

        await folder.OpenAsync(FolderAccess.ReadWrite);

        var uid = new UniqueId(uniqueId);
        var message = await folder.GetMessageAsync(uid);
        await folder.AddFlagsAsync(uid, MessageFlags.Seen, true);

        await imap.DisconnectAsync(true);

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            return message.HtmlBody;

        // лист без HTML: загортаємо текст у сторінку
        string text = WebUtility.HtmlEncode(message.TextBody ?? "");
        return $"<html><body style=\"font-family: sans-serif; font-size: 14px; white-space: pre-wrap;\">{text}</body></html>";
    }

    // Відправка через SMTP
    public async Task SendEmailAsync(AccountSettings settings, string recipient, string subject, string bodyText, string? attachmentPath = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SparkleMail User", settings.Email));
        message.To.Add(new MailboxAddress("", recipient));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = bodyText };

        if (!string.IsNullOrEmpty(attachmentPath))
        {
            await builder.Attachments.AddAsync(attachmentPath);
        }

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(settings.SmtpServer, settings.SmtpPort, SecureSocketOptions.Auto);
        await smtp.AuthenticateAsync(settings.Email, settings.Password);

        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}