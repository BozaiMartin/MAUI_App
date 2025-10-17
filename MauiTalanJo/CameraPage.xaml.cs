using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Camera;
using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui
{
    public partial class CameraPage : ContentPage
    {
        private bool isCapturing = false;

        public CameraPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Engedély szükséges", "A kamera használatához engedély szükséges.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Kis várakozás, hogy a kamera elinduljon
            await Task.Delay(1000);

            // Automatikus kép készítés
            await CaptureAutomatically();
        }

        private async Task CaptureAutomatically()
        {
            if (isCapturing)
                return;

            isCapturing = true;

            try
            {
                await cameraView.CaptureImage(CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Kép készítés sikertelen: {ex.Message}", "OK");
                isCapturing = false;
            }
        }

        private async void CameraView_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            string? filePath = null;

            try
            {
                string fileName = $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (MemoryStream memoryStream = new())
                {
                    await e.Media.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    using FileStream fileStream = File.Create(filePath);
                    await memoryStream.CopyToAsync(fileStream);
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        Page? root = Application.Current?.MainPage;

                        if (root is NavigationPage nav)
                        {
                            if (nav.Navigation.NavigationStack.FirstOrDefault(p => p is MainPage) is MainPage mainNav)
                                await mainNav.InvokeKepFeldolgozAsync(filePath);
                            else
                                await DisplayAlert("Hiba", "Nem található a fõoldal a navigációs veremben.", "OK");
                        }
                        else if (root is Shell shell &&
                                 shell.CurrentPage is MainPage shellMain)
                        {
                            await shellMain.InvokeKepFeldolgozAsync(filePath);
                        }
                        else if (root is MainPage main)
                        {
                            await main.InvokeKepFeldolgozAsync(filePath);
                        }
                        else
                        {
                            await DisplayAlert("Hiba", "Nem található a fõoldal az OCR feldolgozáshoz.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Hiba", $"Feldolgozás közben hiba: {ex.Message}", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Hiba", $"Kép feldolgozási hiba: {ex.Message}", "OK");
                });
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch { }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Navigation.NavigationStack.Count > 1)
                            await Navigation.PopAsync();
                    }
                    catch { }
                });
            }
        }
    }
}
