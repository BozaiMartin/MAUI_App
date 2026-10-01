namespace KmKiolvasasMaui.Kezelok
{
    public static class AlkalmazasBeallitasok
    {
        private const string TelephelyKulcs = "Telephely";

        public static string Telephely
        {
            get
            {
                return Preferences.Default.Get(TelephelyKulcs, "");
            }
        }

        public static void TelephelyBeallitas(string telephely)
        {
            Preferences.Default.Set(TelephelyKulcs, telephely.Trim());
        }
    }
}