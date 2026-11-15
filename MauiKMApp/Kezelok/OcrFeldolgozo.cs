using KmKiolvasasMaui.Adat_Szerkezet;
using Plugin.Maui.OCR;
using System.Diagnostics;

namespace KmKiolvasasMaui.Kezelok
{
    public static class OcrFeldolgozo
    {
        public static async Task<OcrKinyertAdat?> FelismeresAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            Stopwatch stopper = Stopwatch.StartNew();

            string? teljesSzoveg = await OcrSzovegAsync(imageBytes);

            OcrKinyertAdat eredmeny = OcrAdatKinyero.AlapAdatokKinyerese(teljesSzoveg ?? "");

            bool hasznaltElofeldolgozast = false;

            if (!eredmeny.Datum.HasValue || !eredmeny.Palyaszam.HasValue)
            {
                try
                {
                    byte[] teljesElofeldolgozott = KepElofeldolgozo.OcrValtozatKeszites(imageBytes);

                    string? elofeldolgozottSzoveg = await OcrSzovegAsync(teljesElofeldolgozott);

                    OcrKinyertAdat masodik = OcrAdatKinyero.AlapAdatokKinyerese(elofeldolgozottSzoveg ?? "");

                    if (!eredmeny.Datum.HasValue)
                        eredmeny.Datum = masodik.Datum;

                    if (!eredmeny.Palyaszam.HasValue)
                        eredmeny.Palyaszam = masodik.Palyaszam;

                    hasznaltElofeldolgozast = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Teljes kép előfeldolgozási hiba: {ex}");
                }
            }


            byte[] napiRegio = KepElofeldolgozo.RegioKivagas(imageBytes, balArany: 0.02, felsoArany: 0.20, jobbArany: 0.50, alsoArany: 0.80);

            string? napiSzoveg = await OcrSzovegAsync(napiRegio);

            eredmeny.NapiKm = OcrAdatKinyero.KilometerErtekKinyerese(napiSzoveg ?? "");

            if (!eredmeny.NapiKm.HasValue)
            {
                try
                {
                    byte[] napiElofeldolgozott = KepElofeldolgozo.OcrValtozatKeszites(napiRegio);

                    string? napiMasodikSzoveg = await OcrSzovegAsync(napiElofeldolgozott);

                    eredmeny.NapiKm = OcrAdatKinyero.KilometerErtekKinyerese(napiMasodikSzoveg ?? "");

                    hasznaltElofeldolgozast = true;

                    napiSzoveg += "\n--- ELŐFELDOLGOZOTT ---\n" +  napiMasodikSzoveg;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Napi ROI előfeldolgozási hiba: {ex}");
                }
            }


            byte[] osszesRegio = KepElofeldolgozo.RegioKivagas(imageBytes, balArany: 0.50, felsoArany: 0.20, jobbArany: 0.98, alsoArany: 0.80);

            string? osszesSzoveg = await OcrSzovegAsync(osszesRegio);

            eredmeny.OsszKm = OcrAdatKinyero.KilometerErtekKinyerese(osszesSzoveg ?? "");

            if (!eredmeny.OsszKm.HasValue)
            {
                try
                {
                    byte[] osszesElofeldolgozott = KepElofeldolgozo.OcrValtozatKeszites(osszesRegio);

                    string? osszesMasodikSzoveg = await OcrSzovegAsync(osszesElofeldolgozott);

                    eredmeny.OsszKm = OcrAdatKinyero.KilometerErtekKinyerese(osszesMasodikSzoveg ?? "");

                    hasznaltElofeldolgozast =
                        true;

                    osszesSzoveg += "\n--- ELŐFELDOLGOZOTT ---\n" + osszesMasodikSzoveg;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Összes ROI előfeldolgozási hiba: {ex}");
                }
            }

            eredmeny.NyersOcrSzoveg = "=== TELJES KÉP ===\n" + (teljesSzoveg ?? "") + "\n\n=== NAPI ROI ===\n" + (napiSzoveg ?? "") + "\n\n=== ÖSSZES ROI ===\n" + (osszesSzoveg ?? "");

            eredmeny.FelismeresiForras = hasznaltElofeldolgozast ? "ROI + előfeldolgozás" : "ROI";

            eredmeny.Bizonytalan = false;

            eredmeny.Figyelmeztetesek.Clear();

            if (!eredmeny.Datum.HasValue)
            {
                eredmeny.Bizonytalan = true;

                eredmeny.Figyelmeztetesek.Add("A dátum nem volt felismerhető.");
            }

            if (!eredmeny.Palyaszam.HasValue)
            {
                eredmeny.Bizonytalan = true;

                eredmeny.Figyelmeztetesek.Add("A pályaszám nem volt felismerhető.");
            }

            if (!eredmeny.NapiKm.HasValue)
            {
                eredmeny.Bizonytalan = true;

                eredmeny.Figyelmeztetesek.Add("A napi kilométerérték nem volt egyértelműen felismerhető.");
            }

            if (!eredmeny.OsszKm.HasValue)
            {
                eredmeny.Bizonytalan = true;

                eredmeny.Figyelmeztetesek.Add("Az összes kilométerérték nem volt egyértelműen felismerhető.");
            }

            if (eredmeny.NapiKm.HasValue && eredmeny.OsszKm.HasValue && eredmeny.NapiKm.Value > eredmeny.OsszKm.Value)
            {
                eredmeny.Bizonytalan = true;

                eredmeny.Figyelmeztetesek.Add("A napi kilométerérték nagyobb az összesített futásteljesítménynél.");
            }

            eredmeny.MegbizhatosagiPont = 0;

            if (eredmeny.Datum.HasValue)
                eredmeny.MegbizhatosagiPont += 2;

            if (eredmeny.Palyaszam.HasValue)
                eredmeny.MegbizhatosagiPont += 2;

            if (eredmeny.NapiKm.HasValue)
                eredmeny.MegbizhatosagiPont += 3;

            if (eredmeny.OsszKm.HasValue) 
                eredmeny.MegbizhatosagiPont += 3;

            stopper.Stop();

            eredmeny.FeldolgozasiIdoMs = stopper.ElapsedMilliseconds;

            Debug.WriteLine(eredmeny.NyersOcrSzoveg);

            return eredmeny;
        }

        private static async Task<string?> OcrSzovegAsync(byte[] kep)
        {
            try
            {
                OcrResult? result = await OcrPlugin.Default.RecognizeTextAsync(kep, true);

                if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.AllText))
                    return null;

                return result.AllText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OCR futtatási hiba: {ex}");

                return null;
            }
        }
    }
}