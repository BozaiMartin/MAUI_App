using KmKiolvasasMaui.Kezelok;

namespace KmKiolvasasMaui.Adatbazis
{
    public static class AdatbazisInicializalo
    {
        private static bool _inicializalva;
        private static readonly SemaphoreSlim _zar = new(1, 1);

        public static async Task InicializalasAsync()
        {
            if (_inicializalva)
                return;

            await _zar.WaitAsync();

            try
            {
                if (_inicializalva)
                    return;

                SQL_Kezelo_Bejelentkezes bejelentkezesDb = new();

                SQL_Kezelo_Dolgozo dolgozoDb = new ();

                await SQL_Kezelo_Bejelentkezes.InicializalasAsync();
                await SQL_Kezelo_Dolgozo.InicializalasAsync();

                _inicializalva = true;
            }
            finally
            {
                _zar.Release();
            }
        }
    }
}