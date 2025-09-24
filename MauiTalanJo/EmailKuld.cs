using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Mail;
using System.Threading.Tasks;

public static class EmailKuld
{
    public static async Task KuldesAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Kiolvasó App", "bozaimartin@gmail.com"));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

        // Gmail esetén: teljes e-mail + alkalmazásjelszó
        await client.AuthenticateAsync("bozaimartin@gmail.com", "Demonka0801");

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
