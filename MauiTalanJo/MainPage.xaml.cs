using Plugin.Maui.OCR;
using System.Threading.Tasks;

namespace MauiTalanJo
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
                    if (!ocrResult.Success)
                    {
                        await DisplayAlert("Hibás", "nincs OCR", "OK");
                        return;
                    }
                    await DisplayAlert("Eredmény", ocrResult.AllText, "OK");
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
                    if (!ocrResult.Success)
                    {
                        await DisplayAlert("Hibás", "nincs OCR", "OK");
                        return;
                    }
                    await DisplayAlert("Eredmény", ocrResult.AllText, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred while taking the photo: {ex.Message}", "OK");
                return;
            }
        }
    }

}
