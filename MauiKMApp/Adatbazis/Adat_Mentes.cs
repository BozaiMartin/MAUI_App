namespace KmKiolvasasMaui.Adatbazis
{
    internal class Adat_Mentes
    {
        private readonly Adatbazis_Kezelo _kezelo = new();

        public async Task MentAdatotAsync(KiolvasottAdat adat)
        {
            await _kezelo.MentesAsync(adat);
        }
    }
}
