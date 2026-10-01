using ZXing.Net.Maui;

namespace KmKiolvasasMaui
{
    public partial class QrBeolvasoOldal : ContentPage
    {
        private readonly Action<string> _sikeresBeolvasas;

        private readonly bool _regisztraciosMod;

        private int _feldolgozasFolyamatban;

        public QrBeolvasoOldal(
            Action<string> sikeresBeolvasas,
            bool regisztraciosMod = false)
        {
            InitializeComponent();

            _sikeresBeolvasas =
                sikeresBeolvasas
                ?? throw new ArgumentNullException(
                    nameof(sikeresBeolvasas));

            _regisztraciosMod =
                regisztraciosMod;

            QrKamera.Options =
                new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.TwoDimensional,
                    AutoRotate = true,
                    Multiple = false
                };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _feldolgozasFolyamatban = 0;

            QrKamera.IsDetecting = true;
        }

        protected override void OnDisappearing()
        {
            QrKamera.IsDetecting = false;

            QrKamera.IsTorchOn = false;

            base.OnDisappearing();
        }

        private void QrKamera_BarcodesDetected(
            object sender,
            BarcodeDetectionEventArgs e)
        {
            if (Interlocked.CompareExchange(
                    ref _feldolgozasFolyamatban,
                    1,
                    0) != 0)
            {
                return;
            }

            string? qrTartalom =
                e.Results?
                    .FirstOrDefault()?
                    .Value;

            if (string.IsNullOrWhiteSpace(qrTartalom))
            {
                Interlocked.Exchange(
                    ref _feldolgozasFolyamatban,
                    0);

                return;
            }

            if (_regisztraciosMod)
            {
                _ = MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        try
                        {
                            QrKamera.IsDetecting = false;

                            QrKamera.IsTorchOn = false;

                            _sikeresBeolvasas(
                                qrTartalom);

                            await Navigation.PopAsync();
                        }
                        catch
                        {
                            Interlocked.Exchange(
                                ref _feldolgozasFolyamatban,
                                0);

                            QrKamera.IsDetecting = true;

                            throw;
                        }
                    });

                return;
            }

            if (!QrEllenorzes(
                    qrTartalom,
                    out string dolgozoSzam))
            {
                _ = MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        QrKamera.IsDetecting = false;

                        await DisplayAlert(
                            "Érvénytelen QR-kód",
                            "Ez nem KM Kiolvasó belépési QR-kód.",
                            "OK");

                        Interlocked.Exchange(
                            ref _feldolgozasFolyamatban,
                            0);

                        QrKamera.IsDetecting = true;
                    });

                return;
            }

            _ = MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    try
                    {
                        QrKamera.IsDetecting = false;

                        QrKamera.IsTorchOn = false;

                        _sikeresBeolvasas(
                            dolgozoSzam);

                        await Navigation.PopAsync();
                    }
                    catch
                    {
                        Interlocked.Exchange(
                            ref _feldolgozasFolyamatban,
                            0);

                        QrKamera.IsDetecting = true;

                        throw;
                    }
                });
        }

        private static bool QrEllenorzes(
            string? qrTartalom,
            out string dolgozoSzam)
        {
            dolgozoSzam = "";

            if (string.IsNullOrWhiteSpace(qrTartalom))
                return false;

            const string prefix =
                "KMLOGIN:1:";

            if (!qrTartalom.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
            {
                return false;
            }

            string ertek =
                qrTartalom[prefix.Length..]
                    .Trim();

            if (string.IsNullOrWhiteSpace(ertek))
                return false;

            if (ertek.Length > 20)
                return false;

            if (!ertek.All(char.IsDigit))
                return false;

            dolgozoSzam = ertek;

            return true;
        }

        private void VakuButton_Clicked(
            object sender,
            EventArgs e)
        {
            QrKamera.IsTorchOn =
                !QrKamera.IsTorchOn;

            VakuButton.Text =
                QrKamera.IsTorchOn
                    ? "Vaku ki"
                    : "Vaku";
        }

        private async void MegseButton_Clicked(
            object sender,
            EventArgs e)
        {
            QrKamera.IsDetecting = false;

            QrKamera.IsTorchOn = false;

            await Navigation.PopAsync();
        }
    }
}