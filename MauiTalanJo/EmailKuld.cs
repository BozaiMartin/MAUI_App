using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Security;

public static class EmailKuld
{
    public static async Task KuldesAsync(string to, string subject, string body)
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress("Kiolvasó App", "bozaimartin2@gmail.com"));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using SmtpClient client = new SmtpClient();

        // SSL cert ellenőrzés lazítása
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;

        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync("bozaimartin@gmail.com", "rdyt vhaq bepo ikzz");

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
