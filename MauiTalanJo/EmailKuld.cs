using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace KmKiolvasasMaui
{
    public static class EmailKuld
    {
        // --- KONFIG --- (tedd át később beállításba / SecureStorage-be) -----------------
        private const string GmailUser = "bozaimartin@gmail.com";     // Gmail címed (hiteles feladó)
        private const string GmailAppPassword = "rdyt vhaq bepo ikzz"; // Gmail alkalmazásjelszó
        private const string ReplyToNev = "Km Kiolvasás (BKV)";
        private const string ReplyToEmail = "bozaim@bkv.hu";           // Válaszcím a BKV-s postafiókra
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// CSV fájl küldése automatikusan Gmailen keresztül (From = Gmail, Reply-To = BKV).
        /// </summary>
        public static async Task KuldesCsvAsync(string to, string subject, string csvTartalom)
        {
            // 1) MIME üzenet összeállítás
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Km Kiolvasás Adatok", GmailUser)); // hiteles feladó = Gmail
            message.ReplyTo.Add(new MailboxAddress(ReplyToNev, ReplyToEmail));      // válaszcím = BKV
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            // 2) CSV fájl létrehozása a cache-ben
            string fileName = $"KmAdatok_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllText(filePath, csvTartalom, Encoding.UTF8);

            // 3) Törzs + csatolmány
            var builder = new BodyBuilder
            {
                TextBody =
                    $"Tisztelt Címzett!\n\n" +
                    $"A {DateTime.Now:yyyy.MM.dd} napon kiolvasott adatok a csatolt CSV fájlban találhatók.\n\n" +
                    $"Üdvözlettel:\nKm Kiolvasó alkalmazás"
            };

            var contentType = new ContentType("text", "csv")
            {
                Charset = "utf-8",
                Name = fileName
            };

            builder.Attachments.Add(filePath, contentType);
            message.Body = builder.ToMessageBody();

            // 4) SMTP küldés (587 → 465 fallback), OCSP/CRL ellenőrzés kikapcsolva
            //    (csak a revocation check-et kapcsoljuk ki; a többi validáció megmarad)
            try
            {
                using var client = new SmtpClient
                {
                    CheckCertificateRevocation = false // OCSP/CRL hiba megkerülése
                };

                // NE használj mindenre-igaz callbacket!
                // client.ServerCertificateValidationCallback = (s, c, h, e) => true;  // TILOS

                // Első próbálkozás: StartTLS (587)
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(GmailUser, GmailAppPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception)
            {
                // Fallback: SSL a 465-ös porton
                using var client = new SmtpClient
                {
                    CheckCertificateRevocation = false
                };

                await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(GmailUser, GmailAppPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            finally
            {
                // 5) Ideiglenes fájl törlése
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { /* ignore */ }
            }
        }
    }
}
