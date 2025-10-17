using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Camera;
using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui
{
    public partial class CameraPage : ContentPage
    {
        private bool isCapturing = false;
        private bool allDataFound = false;
        private int retryCount = 0;
        private const int MaxRetries = 10; // biztonsági limit

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
                await Navigation.PopAsync();
                return;
            }

            // kis késleltetés, amíg a kamera inicializálódik
            await Task.Delay(1000);
            await StartAutoCaptureLoop();
        }

        private async Task StartAutoCaptureLoop()
        {
            allDataFound = false;
            retryCount = 0;

            while (!allDataFound && retryCount < MaxRetries)
            {
                retryCount++;
                await CaptureAutomatically();
                // kis várakozás az OCR feldolgozásra
                await Task.Delay(2500);
            }

            // ha már minden adat megvan, visszatér a főoldalra
            if (allDataFound)
                await SafeBack();
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
            catch
            {
                // kamera hiba → megpróbálja újra
            }
            finally
            {
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

                // OCR feldolgozás a főoldalon
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        Page? root = Application.Current?.MainPage;
                        MainPage? main = null;

                        if (root is NavigationPage nav)
                            main = nav.Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault();
                        else if (root is Shell shell)
                            main = shell.CurrentPage as MainPage;
                        else if (root is MainPage mp)
                            main = mp;

                        if (main != null)
                        {
                            bool success = await main.InvokeKepFeldolgozAutomatikusAsync(filePath);
                            if (success)
                                allDataFound = true;
                        }
                    }
                    catch
                    {
                        // OCR feldolgozási hiba → újra próbálkozik
                    }
                });
            }
            catch
            {
                // fájlhiba → újra próbálkozik
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch { }
            }
        }

        private async Task SafeBack()
        {
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
