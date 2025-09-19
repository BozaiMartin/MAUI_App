using Plugin.Maui.OCR;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KmKiolvasasMaui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await OcrPlugin.Default.InitAsync();
        }
        private async void SelectBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                FileResult? pickResult = await MediaPicker.Default.PickPhotoAsync();
                if (pickResult != null)
                {
                    using Stream imageAsStream = await pickResult.OpenReadAsync();
                    byte[] imageAsBytes = new byte[imageAsStream.Length];
                    await imageAsStream.ReadAsync(imageAsBytes);
                   OcrResult? ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes,true);
                    //if (!ocrResult.Success)
                    //{
                    //    await DisplayAlert("Hibás", "nincs OCR", "OK");
                    //    return;
                    //}
                    //await DisplayAlert("Eredmény", ocrResult.AllText, "OK");
                    if (ocrResult.Success)
                    {
                        var adatok = ParseOcrText(ocrResult.AllText);

                        string tabla =
                            $"Dátum: {adatok.datum}\n" +
                            $"Pályaszám: {adatok.palyaszam}\n\n" +
                            $"| Típus   | Megtett út |\n" +
                            $"|---------|------------|\n" +
                            $"| NAPI    | {adatok.napiKm} km |\n" +
                            $"| ÖSSZES  | {adatok.osszKm} km |";

                        await DisplayAlert("Kiolvasott adatok", tabla, "OK");
                    }

                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while picking the photo: {ex.Message}", "OK");
                return;
            }
        }

        private async void PictureBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                FileResult? pickResult = await MediaPicker.Default.CapturePhotoAsync();
                if (pickResult != null)
                {
                    using Stream imageAsStream = await pickResult.OpenReadAsync();
                    byte[] imageAsBytes = new byte[imageAsStream.Length];
                    await imageAsStream.ReadAsync(imageAsBytes);
                    OcrResult? ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes, true);
                    //if (!ocrResult.Success)
                    //{
                    //    await DisplayAlert("Hibás", "nincs OCR", "OK");
                    //    return;
                    //}
                    //await DisplayAlert("Eredmény", ocrResult.AllText, "OK");
                    if (ocrResult.Success)
                    {
                        var adatok = ParseOcrText(ocrResult.AllText);

                        string tabla =
                            $"Dátum: {adatok.datum}\n" +
                            $"Pályaszám: {adatok.palyaszam}\n\n" +
                            $"| Típus   | Megtett út |\n" +
                            $"|---------|------------|\n" +
                            $"| NAPI    | {adatok.napiKm} km |\n" +
                            $"| ÖSSZES  | {adatok.osszKm} km |";

                        await DisplayAlert("Kiolvasott adatok", tabla, "OK");
                    }


                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while taking the photo: {ex.Message}", "OK");
                return;
            }
        }
        private (string datum, string palyaszam, string napiKm, string osszKm) ParseOcrText(string ocrText)
        {
            string datum = "";
            string palyaszam = "";
            string napiKm = "";
            string osszKm = "";

            var sorok = ocrText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sor in sorok)
            {
                // dátum keresés: 2025.09.19. 09:06:53
                var datumMatch = Regex.Match(sor, @"\d{4}\.\d{2}\.\d{2}\.\s+\d{2}:\d{2}:\d{2}");
                if (datumMatch.Success)
                {
                    datum = datumMatch.Value;
                }

                // pályaszám: csak szám, 3-5 számjegy
                if (Regex.IsMatch(sor.Trim(), @"^\d{3,5}$"))
                {
                    palyaszam = sor.Trim();
                }

                // megtett km-ek
                if (sor.ToLower().Contains("megtett") && sor.ToLower().Contains("km"))
                {
                    string szam = new string(sor.Where(char.IsDigit).ToArray());
                    if (napiKm == "")
                        napiKm = szam;  // első = napi
                    else
                        osszKm = szam;  // második = összes
                }
            }

            return (datum, palyaszam, napiKm, osszKm);
        }



    }

}
