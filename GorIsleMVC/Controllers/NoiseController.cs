using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GorIsleMVC.Controllers
{
    public class NoiseController : Controller
    {
        private readonly Random _random = new Random();
        private readonly IWebHostEnvironment _environment;

        public NoiseController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddNoise(IFormFile imageFile, int noiseDensity)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            double densityValue = Math.Max(0, Math.Min(100, noiseDensity)) / 100.0;

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap noisyImage = null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Png);

                bitmap = new Bitmap(originalImage);
                noisyImage = ApplySaltAndPepperNoise(bitmap, densityValue);

                var resultFileName = $"noisy_{noiseDensity}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                noisyImage.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["Density"] = noiseDensity.ToString();
                TempData["ProcessType"] = "noise";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                noisyImage?.Dispose();
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveNoise(IFormFile imageFile, int filterSize)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap filteredImage = null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Png);

                bitmap = new Bitmap(originalImage);
                filteredImage = ApplyMedianFilter(bitmap, filterSize);

                var resultFileName = $"filtered_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                filteredImage.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["FilterSize"] = filterSize;
                TempData["ProcessType"] = "filter";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                filteredImage?.Dispose();
            }
        }

        private Bitmap ApplySaltAndPepperNoise(Bitmap sourceBitmap, double density)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // ARRAY TABANLI YAKLAŞIM
            byte[,,] sourcePixels = new byte[width, height, 4]; // ARGB
            byte[,,] resultPixels = new byte[width, height, 4]; // ARGB

            // ADIM 1: Bitmap'ten array'e piksel aktarımı
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1] = pixel.R; // Red
                    sourcePixels[x, y, 2] = pixel.G; // Green
                    sourcePixels[x, y, 3] = pixel.B; // Blue

                    // Önce orijinal değerleri kopyala
                    resultPixels[x, y, 0] = pixel.A;
                    resultPixels[x, y, 1] = pixel.R;
                    resultPixels[x, y, 2] = pixel.G;
                    resultPixels[x, y, 3] = pixel.B;
                }
            }

            // ADIM 2: Array üzerinde salt & pepper noise ekleme
            int totalPixels = width * height;
            int noisePixels = (int)(totalPixels * density);

            for (int i = 0; i < noisePixels; i++)
            {
                int x = _random.Next(width);
                int y = _random.Next(height);

                // Salt (beyaz) veya Pepper (siyah) rastgele seç
                if (_random.NextDouble() < 0.5)
                {
                    // Pepper (siyah)
                    resultPixels[x, y, 1] = 0; // Red = 0
                    resultPixels[x, y, 2] = 0; // Green = 0
                    resultPixels[x, y, 3] = 0; // Blue = 0
                }
                else
                {
                    // Salt (beyaz)
                    resultPixels[x, y, 1] = 255; // Red = 255
                    resultPixels[x, y, 2] = 255; // Green = 255
                    resultPixels[x, y, 3] = 255; // Blue = 255
                }
            }

            // ADIM 3: Array'den yeni Bitmap oluştur
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = resultPixels[x, y, 0];
                    byte red = resultPixels[x, y, 1];
                    byte green = resultPixels[x, y, 2];
                    byte blue = resultPixels[x, y, 3];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }

        private Bitmap ApplyMedianFilter(Bitmap sourceBitmap, int filterSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int offset = filterSize / 2;

            // ARRAY TABANLI YAKLAŞIM
            byte[,,] sourcePixels = new byte[width, height, 4]; // ARGB
            byte[,,] resultPixels = new byte[width, height, 4]; // ARGB

            // ADIM 1: Bitmap'ten array'e piksel aktarımı
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1] = pixel.R; // Red
                    sourcePixels[x, y, 2] = pixel.G; // Green
                    sourcePixels[x, y, 3] = pixel.B; // Blue

                    // Varsayılan olarak orijinal değerleri kopyala
                    resultPixels[x, y, 0] = pixel.A;
                    resultPixels[x, y, 1] = pixel.R;
                    resultPixels[x, y, 2] = pixel.G;
                    resultPixels[x, y, 3] = pixel.B;
                }
            }

            // ADIM 2: Array üzerinde median filter işlemi
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    List<byte> redValues = new List<byte>();
                    List<byte> greenValues = new List<byte>();
                    List<byte> blueValues = new List<byte>();

                    // Komşu pikselleri array'den topla
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            redValues.Add(sourcePixels[currentX, currentY, 1]);
                            greenValues.Add(sourcePixels[currentX, currentY, 2]);
                            blueValues.Add(sourcePixels[currentX, currentY, 3]);
                        }
                    }

                    // Median hesapla
                    byte medianR = GetMedian(redValues);
                    byte medianG = GetMedian(greenValues);
                    byte medianB = GetMedian(blueValues);

                    // Sonucu array'e kaydet
                    resultPixels[x, y, 0] = sourcePixels[x, y, 0]; // Alpha aynı kalır
                    resultPixels[x, y, 1] = medianR;
                    resultPixels[x, y, 2] = medianG;
                    resultPixels[x, y, 3] = medianB;
                }
            }

            // ADIM 3: Kenar pikselleri için median filter (basitleştirilmiş)
            ProcessImageEdgesArray(sourcePixels, resultPixels, width, height, filterSize);

            // ADIM 4: Array'den yeni Bitmap oluştur
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = resultPixels[x, y, 0];
                    byte red = resultPixels[x, y, 1];
                    byte green = resultPixels[x, y, 2];
                    byte blue = resultPixels[x, y, 3];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }

        private void ProcessImageEdgesArray(byte[,,] sourcePixels, byte[,,] resultPixels,
                                          int width, int height, int filterSize)
        {
            int offset = filterSize / 2;

            // Üst ve alt kenarlar
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < offset; y++)
                {
                    // Üst kenar - kullanılabilir komşuları topla
                    List<byte> redValues = new List<byte>();
                    List<byte> greenValues = new List<byte>();
                    List<byte> blueValues = new List<byte>();

                    for (int i = Math.Max(0, x - offset); i <= Math.Min(width - 1, x + offset); i++)
                    {
                        for (int j = 0; j <= Math.Min(height - 1, y + offset); j++)
                        {
                            redValues.Add(sourcePixels[i, j, 1]);
                            greenValues.Add(sourcePixels[i, j, 2]);
                            blueValues.Add(sourcePixels[i, j, 3]);
                        }
                    }

                    if (redValues.Count > 0)
                    {
                        resultPixels[x, y, 1] = GetMedian(redValues);
                        resultPixels[x, y, 2] = GetMedian(greenValues);
                        resultPixels[x, y, 3] = GetMedian(blueValues);

                        // Alt kenar için de aynı işlemi yap
                        int bottomY = height - 1 - y;
                        if (bottomY != y) // Çakışmayı önle
                        {
                            resultPixels[x, bottomY, 1] = GetMedian(redValues);
                            resultPixels[x, bottomY, 2] = GetMedian(greenValues);
                            resultPixels[x, bottomY, 3] = GetMedian(blueValues);
                        }
                    }
                }
            }

            // Sol ve sağ kenarlar
            for (int y = offset; y < height - offset; y++)
            {
                for (int x = 0; x < offset; x++)
                {
                    List<byte> redValues = new List<byte>();
                    List<byte> greenValues = new List<byte>();
                    List<byte> blueValues = new List<byte>();

                    for (int i = 0; i <= Math.Min(width - 1, x + offset); i++)
                    {
                        for (int j = Math.Max(0, y - offset); j <= Math.Min(height - 1, y + offset); j++)
                        {
                            redValues.Add(sourcePixels[i, j, 1]);
                            greenValues.Add(sourcePixels[i, j, 2]);
                            blueValues.Add(sourcePixels[i, j, 3]);
                        }
                    }

                    if (redValues.Count > 0)
                    {
                        resultPixels[x, y, 1] = GetMedian(redValues);
                        resultPixels[x, y, 2] = GetMedian(greenValues);
                        resultPixels[x, y, 3] = GetMedian(blueValues);

                        // Sağ kenar için de aynı işlemi yap
                        int rightX = width - 1 - x;
                        if (rightX != x) // Çakışmayı önle
                        {
                            resultPixels[rightX, y, 1] = GetMedian(redValues);
                            resultPixels[rightX, y, 2] = GetMedian(greenValues);
                            resultPixels[rightX, y, 3] = GetMedian(blueValues);
                        }
                    }
                }
            }
        }

        private byte GetMedian(List<byte> values)
        {
            if (values.Count == 0) return 0;

            values.Sort(); // LINQ Sort daha hızlı

            int n = values.Count;
            if (n % 2 == 0)
            {
                return (byte)((values[n / 2 - 1] + values[n / 2]) / 2);
            }
            else
            {
                return values[n / 2];
            }
        }
    }
}