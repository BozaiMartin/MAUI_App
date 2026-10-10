using SkiaSharp;

namespace KmKiolvasasMaui.Kezelok
{
    public static class KepElofeldolgozo
    {
        private const int MaxSzelesseg = 1800;
        private const int MinSzelesseg = 1000;

        public static byte[] OcrValtozatKeszites(byte[] eredetiKep)
        {
            if (eredetiKep == null || eredetiKep.Length == 0)
                throw new ArgumentException("A kép nem lehet üres.", nameof(eredetiKep));

            using SKBitmap? dekodolt = SKBitmap.Decode(eredetiKep);

            if (dekodolt == null || dekodolt.Width <= 0 || dekodolt.Height <= 0)
                throw new InvalidOperationException("A kép nem dekódolható.");

            int celSzelesseg = dekodolt.Width;

            if (celSzelesseg > MaxSzelesseg)
                celSzelesseg = MaxSzelesseg;

            else if (celSzelesseg < MinSzelesseg)
                celSzelesseg = MinSzelesseg;

            int celMagassag = Math.Max(1, (int)Math.Round(dekodolt.Height * (celSzelesseg / (double)dekodolt.Width)));

            using SKBitmap atmeretezett = celSzelesseg == dekodolt.Width ? dekodolt.Copy() : dekodolt.Resize(new SKImageInfo(
                celSzelesseg,
                celMagassag,
                SKColorType.Rgba8888,
                SKAlphaType.Opaque),
                SKFilterQuality.Medium) ?? throw new InvalidOperationException("A kép átméretezése nem sikerült.");

            SKColor[] pixelek = atmeretezett.Pixels;

            byte[] szurke = new byte[pixelek.Length];

            long intenzitasOsszeg = 0;

            // SZÜRKEÁRNYALAT
            for (int i = 0; i < pixelek.Length; i++)
            {
                SKColor pixel = pixelek[i];

                byte ertek = (byte)Math.Clamp((int)Math.Round(
                    0.2126 * pixel.Red +
                    0.7152 * pixel.Green +
                    0.0722 * pixel.Blue),
                    0,
                    255);

                szurke[i] = ertek;

                intenzitasOsszeg += ertek;
            }

            int width = atmeretezett.Width;

            int height = atmeretezett.Height;

            // ENYHE 3x3 ZAJCSÖKKENTÉS
            byte[] vizszintes = new byte[szurke.Length];

            byte[] zajszurt = new byte[szurke.Length];

            // vízszintes átlagolás
            for (int y = 0; y < height; y++)
            {
                int sorKezdet = y * width;

                for (int x = 0; x < width; x++)
                {
                    int sum = szurke[sorKezdet + x];

                    int count = 1;

                    if (x > 0)
                    {
                        sum += szurke[sorKezdet + x - 1];

                        count++;
                    }

                    if (x + 1 < width)
                    {
                        sum += szurke[sorKezdet + x + 1];

                        count++;
                    }

                    vizszintes[sorKezdet + x] = (byte)(sum / count);
                }
            }

            // függőleges átlagolás
            for (int y = 0; y < height; y++)
            {
                int sorKezdet = y * width;

                for (int x = 0; x < width; x++)
                {
                    int sum = vizszintes[sorKezdet + x];

                    int count = 1;

                    if (y > 0)
                    {
                        sum += vizszintes[sorKezdet - width + x];

                        count++;
                    }

                    if (y + 1 < height)
                    {
                        sum += vizszintes[sorKezdet + width + x];

                        count++;
                    }

                    zajszurt[sorKezdet + x] = (byte)(sum / count);
                }
            }

            // OTSU KÜSZÖB
            int kuszob = OtsuKuszob(zajszurt);

            double atlagIntenzitas = intenzitasOsszeg / (double)pixelek.Length;

            // Ha sötét a kijelző háttere,
            // fekete karaktereket készítünk fehér háttérrel.
            bool sotetHatter = atlagIntenzitas < 128;

            SKColor[] kimenet = new SKColor[zajszurt.Length];

            for (int i = 0; i < zajszurt.Length; i++)
            {
                bool karakter = sotetHatter ? zajszurt[i] >= kuszob : zajszurt[i] < kuszob;

                byte ertek = karakter ? (byte)0 : (byte)255;

                kimenet[i] = new SKColor(ertek, ertek, ertek, 255);
            }

            using SKBitmap eredmeny = new(
                width,
                height,
                SKColorType.Rgba8888,
                SKAlphaType.Opaque);

            eredmeny.Pixels = kimenet;

            using SKImage image = SKImage.FromBitmap(eredmeny);

            using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);

            return png.ToArray();
        }

        private static int OtsuKuszob(byte[] szurke)
        {
            int[] histogram = new int[256];

            foreach (byte ertek in szurke)
                histogram[ertek]++;

            long teljes = szurke.Length;

            long osszeg = 0;

            for (int i = 0; i < histogram.Length; i++)
                osszeg += (long)i * histogram[i];

            long hatterDarab = 0;
            long hatterOsszeg = 0;

            double legnagyobbVariancia = -1;

            int legjobbKuszob = 127;

            for (int t = 0; t < 256; t++)
            {
                hatterDarab += histogram[t];

                if (hatterDarab == 0)
                    continue;

                long eloterDarab = teljes - hatterDarab;

                if (eloterDarab == 0)
                    break;

                hatterOsszeg += (long)t * histogram[t];

                double hatterAtlag = hatterOsszeg / (double)hatterDarab;

                double eloterAtlag = (osszeg - hatterOsszeg) / (double)eloterDarab;

                double kulonbseg = hatterAtlag - eloterAtlag;

                double variancia = hatterDarab * (double)eloterDarab * kulonbseg * kulonbseg;

                if (variancia > legnagyobbVariancia)
                {
                    legnagyobbVariancia = variancia;

                    legjobbKuszob = t;
                }
            }

            return legjobbKuszob;
        }
        public static byte[] RegioKivagas(byte[] eredetiKep, double balArany, double felsoArany, double jobbArany, double alsoArany)
        {
            if (eredetiKep == null || eredetiKep.Length == 0)
                throw new ArgumentException("A kép nem lehet üres.");

            using SKBitmap? eredeti = SKBitmap.Decode(eredetiKep);

            if (eredeti == null)
                throw new InvalidOperationException("A kép nem dekódolható.");

            balArany = Math.Clamp(balArany, 0, 1);
            jobbArany = Math.Clamp(jobbArany, 0, 1);
            felsoArany = Math.Clamp(felsoArany, 0, 1);
            alsoArany = Math.Clamp(alsoArany, 0, 1);

            int x1 = (int)(eredeti.Width * balArany);

            int x2 = (int)(eredeti.Width * jobbArany);

            int y1 = (int)(eredeti.Height * felsoArany);

            int y2 = (int)(eredeti.Height * alsoArany);

            int szelesseg = Math.Max(1, x2 - x1);

            int magassag = Math.Max(1, y2 - y1);

            using SKBitmap kivagott = new(szelesseg, magassag, SKColorType.Rgba8888, SKAlphaType.Opaque);

            using SKCanvas canvas = new(kivagott);

            SKRect forras = new(x1, y1, x2, y2);

            SKRect cel =new(0, 0, szelesseg, magassag);

            canvas.DrawBitmap(eredeti, forras, cel);

            canvas.Flush();

            using SKImage image = SKImage.FromBitmap(kivagott);

            using SKData png = image.Encode(SKEncodedImageFormat.Png,100);

            return png.ToArray();
        }
    }
}