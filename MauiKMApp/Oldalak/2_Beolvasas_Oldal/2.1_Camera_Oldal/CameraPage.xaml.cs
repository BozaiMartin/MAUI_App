using CommunityToolkit.Maui.Views;
using System.Diagnostics;

namespace KmKiolvasasMaui
{
    public partial class CameraPage : ContentPage
    {
        private bool _isCapturing;

        private bool _allDataFound;

        private int _retryCount;

        private const int MaxRetries = 10;

        private TaskCompletionSource<bool>? _aktualisFeldolgozas;

        public CameraPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            PermissionStatus status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Kameraengedély", "A fényképezéshez kameraengedély szükséges.", "OK");
                await SafeBack();
                return;
            }
            await Task.Delay(1000);
            await StartAutoCaptureLoop();
        }

        private async Task StartAutoCaptureLoop()
        {
            _allDataFound = false;

            _retryCount = 0;

            while (!_allDataFound && _retryCount < MaxRetries)
            {
                _retryCount++;

                _aktualisFeldolgozas = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                await CaptureAutomatically();

                bool siker = false;

                try
                {
                    // Nem fix 2,5 másodpercet várunk,
                    // hanem az OCR tényleges végét.
                    siker = await _aktualisFeldolgozas.Task.WaitAsync(TimeSpan.FromSeconds(15));
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("Az OCR feldolgozás időtúllépés miatt megszakadt.");
                }

                if (siker)
                {
                    _allDataFound = true;

                    break;
                }

                await Task.Delay(500);
            }

            if (_allDataFound)
            {
                await SafeBack();

                return;
            }

            await DisplayAlert("Sikertelen felismerés", $"{MaxRetries} próbálkozás után sem sikerült minden szükséges adatot felismerni.", "OK");
            await SafeBack();
        }

        private async Task CaptureAutomatically()
        {
            if (_isCapturing)
                return;

            _isCapturing = true;

            try
            {
                await cameraView.CaptureImage(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Kamerahiba: {ex}");

                _aktualisFeldolgozas?.TrySetResult(false);
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private async void CameraView_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            string? filePath = null;

            bool siker = false;

            try
            {
                string fileName = $"foto_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";

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
                    Page? root = Application.Current?.MainPage;

                    MainPage? main = null;

                    if (root is NavigationPage nav)
                        main = nav.Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault();

                    else if (root is Shell shell)
                        main = shell.CurrentPage as MainPage;

                    else if (root is MainPage mp)
                        main = mp;

                    if (main != null)
                        siker = await main.InvokeKepFeldolgozAutomatikusAsync(filePath);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Kamerakép feldolgozási hiba: {ex}");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ideiglenes kép törlési hiba: {ex}");
                }
                _aktualisFeldolgozas?.TrySetResult(siker);
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
                catch (Exception ex)
                {
                    Debug.WriteLine($"Navigációs hiba: {ex}");
                }
            });
        }
    }
}