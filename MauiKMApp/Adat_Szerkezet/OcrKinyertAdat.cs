namespace KmKiolvasasMaui.Adat_Szerkezet
{
    public sealed class OcrKinyertAdat
    {
        public DateTime? Datum { get; set; }

        public int? Palyaszam { get; set; }

        public int? NapiKm { get; set; }

        public int? OsszKm { get; set; }

        public bool Bizonytalan { get; set; }

        public List<string> Figyelmeztetesek { get; } = [];

        public string FelismeresiForras { get; set; } = "Nyers";

        public string NyersOcrSzoveg { get; set; } = "";

        public long FeldolgozasiIdoMs { get; set; }

        public int MegbizhatosagiPont { get; set; }

        public bool Teljes => Datum.HasValue && Palyaszam.HasValue && NapiKm.HasValue && OsszKm.HasValue;

        public string HianyzoMezokSzovege()
        {
            List<string> hianyzo = [];

            if (!Datum.HasValue)
                hianyzo.Add("dátum");

            if (!Palyaszam.HasValue)
                hianyzo.Add("pályaszám");

            if (!NapiKm.HasValue)
                hianyzo.Add("napi km");

            if (!OsszKm.HasValue)
                hianyzo.Add("összes km");

            return string.Join(", ", hianyzo);
        }
    }
}