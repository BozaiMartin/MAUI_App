using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace KmKiolvasasMaui
{
    public static class EmailKuld
    {
        private const string GmailUser = "bozaimartin@gmail.com";     // Gmail címed (hiteles feladó)
        private const string GmailAppPassword = "rdyt vhaq bepo ikzz"; // Gmail alkalmazásjelszó
        private const string ReplyToNev = "Km Kiolvasás (BKV)";
        private const string ReplyToEmail = "bozaim@bkv.hu";           // Válaszcím a BKV-s postafiókra

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
            BodyBuilder builder = new()
            {
                TextBody =
                "Tisztelt Kolléga!\n\n" +
                $"A {DateTime.Now:yyyyMMdd_HH} napon kiolvasott kilométer-adatokat a mellékelt CSV fájlban találja. " +
                "A dokumentumban minden jármű napi és összes futásteljesítménye szerepel a " +
                "kiolvasás időpontjának megfelelően.\n\n" +
                "Kérjük, a mellékletet csak belső felhasználásra kezelje, és továbbítsa a " +
                "megfelelő feldolgozási helyre, ha szükséges.\n\n" +
                "Amennyiben a fájl megnyitása vagy az adatok feldolgozása során bármilyen " +
                "problémát tapasztal, kérem, jelezze a Km Kiolvasás rendszergazdájának.\n\n" +
                "Köszönjük az együttműködést és jó munkát kívánunk!\n\n" +
                "Üdvözlettel:\n" +
                "Km Kiolvasó alkalmazás\n" +
                "BKV Zrt.\n"
            };


            ContentType contentType = new("text", "csv")
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
                using SmtpClient client = new()
                {
                    CheckCertificateRevocation = false // OCSP/CRL hiba megkerülése
                };

                // Első próbálkozás: StartTLS (587)
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(GmailUser, GmailAppPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception)
            {
                // Fallback: SSL a 465-ös porton
                using SmtpClient client = new()
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
