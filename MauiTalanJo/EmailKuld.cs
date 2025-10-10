using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace KmKiolvasasMaui
{
    public static class EmailKuld
    {
        /// <summary>
        /// CSV fájl küldése automatikusan, SMTP-n keresztül.
        /// </summary>
        public static async Task KuldesCsvAsync(string to, string subject, string csvTartalom)
        {
            // E-mail üzenet összeállítása
            MimeMessage message = new();
            message.From.Add(new MailboxAddress("Km Kiolvasás Adatok", "pozsgaii@bkv.hu"));
            //message.From.Add(new MailboxAddress("Km Kiolvasás Adatok", "bozaim@bkv.hu"));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            // CSV fájl létrehozása a CacheDirectory-ban
            string fileName = $"KmAdatok_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllText(filePath, csvTartalom, Encoding.UTF8);

            // Üzenet-törzs + csatolmány
            BodyBuilder builder = new()
            {
                TextBody = $"Tisztelt Címzett!\n\nA {DateTime.Now:yyyy.MM.dd} napon kiolvasott adatok a csatolt CSV fájlban találhatók.\n\nÜdvözlettel:\nKm Kiolvasó alkalmazás"
            };

            // MIME-típus megadása a csatolmányhoz
            ContentType contentType = new("text", "csv")
            {
                Charset = "utf-8",
                Name = fileName
            };
            builder.Attachments.Add(filePath, contentType);

            message.Body = builder.ToMessageBody();

            // SMTP küldés
            using SmtpClient client = new();

            // opcionális: tanúsítvány-ellenőrzés lazítása, ha a Gmail újraindítaná TLS-ellenőrzés miatt
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("bozaimartin@gmail.com", "rdyt vhaq bepo ikzz");

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
