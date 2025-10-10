using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Camera;
using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui
{
    public partial class CameraPage : ContentPage
    {
        private bool KepetKeszit = false;
        public CameraPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Permissions.RequestAsync<Permissions.Camera>();
        }

        private async void OnCaptureClicked(object sender, EventArgs e)
        {
            if (KepetKeszit)
                return; 

            KepetKeszit = true;
            try
            {
                captureButton.IsEnabled = false;
                await cameraView.CaptureImage(CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Kép készítés sikertelen: {ex.Message}", "OK");
                KepetKeszit = false;
                captureButton.IsEnabled = true;
            }
        }

        private async void CameraView_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            string? filePath = null;

            try
            {
                // Ideiglenes fájlnév
                string fileName = $"foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Stream mentése memóriába, majd fájlba
                using (MemoryStream memoryStream = new())
                {
                    await e.Media.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    using FileStream fileStream = File.Create(filePath);
                    await memoryStream.CopyToAsync(fileStream);
                }

                //  OCR feldolgozás hívása a MainPage-bõl (bármilyen MAUI szerkezet esetén)
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        Page? root = Application.Current?.MainPage;

                        if (root is NavigationPage nav)
                        {
                            // ha NavigationPage-ben vagyunk, próbáljuk megkeresni a MainPage-et
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
                // Fájl törlése (ha létezik)
                try
                {
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch
                {
                    // ha nem sikerül, nem baj — csak ne omljon le
                }

                //  Biztonságos visszalépés a fõoldalra
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Navigation.NavigationStack.Count > 1)
                            await Navigation.PopAsync();
                    }
                    catch
                    {
                        // ha a Navigation stack épp üres, nem baj
                    }
                });
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}
