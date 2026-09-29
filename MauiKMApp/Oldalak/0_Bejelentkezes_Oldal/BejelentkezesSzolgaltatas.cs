using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Kezelok;
using System.Diagnostics;

namespace KmKiolvasasMaui.Adatbazis
{
    public enum BejelentkezesEredmenyTipus
    {
        Sikeres,
        HibasAdatok,
        InaktivDolgozo,
        JelszoCsereSzukseges
    }


    public sealed class BejelentkezesEredmeny
    {
        public BejelentkezesEredmenyTipus Tipus { get; }

        public Adat_User? Felhasznalo { get; }

        public bool Sikeres =>
            Tipus == BejelentkezesEredmenyTipus.Sikeres;


        public BejelentkezesEredmeny(
            BejelentkezesEredmenyTipus tipus,
            Adat_User? felhasznalo = null)
        {
            Tipus = tipus;
            Felhasznalo = felhasznalo;
        }
    }


    public class BejelentkezesSzolgaltatas
    {
        private readonly SQL_Kezelo_Bejelentkezes _bejelentkezesDb;
        private readonly SQL_Kezelo_Dolgozo _dolgozoDb;


        public BejelentkezesSzolgaltatas()
        {
            _bejelentkezesDb =
                new SQL_Kezelo_Bejelentkezes();

            _dolgozoDb =
                new SQL_Kezelo_Dolgozo();
        }


        public async Task<BejelentkezesEredmeny> BejelentkezesAsync(
      string dolgozoSzam,
      string jelszo)
        {
            Stopwatch teljes = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(dolgozoSzam) ||
                string.IsNullOrWhiteSpace(jelszo))
            {
                return new BejelentkezesEredmeny(
                    BejelentkezesEredmenyTipus.HibasAdatok);
            }

            dolgozoSzam = dolgozoSzam.Trim();


            Stopwatch meres = Stopwatch.StartNew();

            Adat_User? user =
                await SQL_Kezelo_Bejelentkezes.FelhasznaloKeresesAsync(dolgozoSzam);

            meres.Stop();

            Debug.WriteLine(
                $"USER KERESÉS: {meres.ElapsedMilliseconds} ms");


            if (user == null)
            {
                return new BejelentkezesEredmeny(
                    BejelentkezesEredmenyTipus.HibasAdatok);
            }


            meres.Restart();

            bool aktiv =
                await _dolgozoDb
                    .DolgozoAktivAsync(dolgozoSzam);

            meres.Stop();

            Debug.WriteLine(
                $"DOLGOZÓ AKTÍV: {meres.ElapsedMilliseconds} ms");


            if (!aktiv)
            {
                return new BejelentkezesEredmeny(
                    BejelentkezesEredmenyTipus.InaktivDolgozo);
            }


            meres.Restart();

            bool joJelszo =
    await Task.Run(() =>
        JelszoKezelo.Ellenorzes(
            jelszo,
            user.JelszoHash,
            user.JelszoSalt));

            meres.Stop();

            Debug.WriteLine(
                $"JELSZÓ HASH: {meres.ElapsedMilliseconds} ms");


            if (!joJelszo)
            {
                return new BejelentkezesEredmeny(
                    BejelentkezesEredmenyTipus.HibasAdatok);
            }


            meres.Restart();

            bool csereKotelezo =
                JelszoKezelo.JelszoCsereKotelezo(
                    user.Frissit,
                    user.JelszoLejarat);

            meres.Stop();

            Debug.WriteLine(
                $"JELSZÓ LEJÁRAT: {meres.ElapsedMilliseconds} ms");


            teljes.Stop();

            Debug.WriteLine(
                $"TELJES BELÉPÉS: {teljes.ElapsedMilliseconds} ms");


            if (csereKotelezo)
            {
                return new BejelentkezesEredmeny(
                    BejelentkezesEredmenyTipus.JelszoCsereSzukseges,
                    user);
            }


            return new BejelentkezesEredmeny(
                BejelentkezesEredmenyTipus.Sikeres,
                user);
        }
    }
}