using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


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

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var originalImage = Image.FromStream(stream))
                    {
                        originalImage.Save(originalPath, ImageFormat.Png);

                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var noisyImage = ApplySaltAndPepperNoise(bitmap, densityValue);

                            var resultFileName = $"noisy_{noiseDensity}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            noisyImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["Density"] = noiseDensity.ToString();
                            TempData["ProcessType"] = "noise";
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
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

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var originalImage = Image.FromStream(stream))
                    {
                        originalImage.Save(originalPath, ImageFormat.Png);

                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var filteredImage = ApplyMedianFilter(bitmap, filterSize);

                            var resultFileName = $"filtered_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            filteredImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["FilterSize"] = filterSize;
                            TempData["ProcessType"] = "filter";
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplySaltAndPepperNoise(Bitmap original, double density)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);

            // Önce orijinal görüntüyü kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result.SetPixel(x, y, original.GetPixel(x, y));
                }
            }

            // Gürültü ekle
            int totalPixels = width * height;
            int noisePixels = (int)(totalPixels * density);

            for (int i = 0; i < noisePixels; i++)
            {
                int x = _random.Next(width);
                int y = _random.Next(height);
                result.SetPixel(x, y, _random.NextDouble() < 0.5 ? Color.Black : Color.White);
            }

            return result;
        }

        private Bitmap ApplyMedianFilter(Bitmap original, int filterSize)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);
            int offset = filterSize / 2;

            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
    
                    int[] redValues = new int[filterSize * filterSize];
                    int[] greenValues = new int[filterSize * filterSize];
                    int[] blueValues = new int[filterSize * filterSize];
                    int index = 0;

                    // Komşu pikselleri topla
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            Color pixel = original.GetPixel(x + i, y + j);
                            redValues[index] = pixel.R;
                            greenValues[index] = pixel.G;
                            blueValues[index] = pixel.B;
                            index++;
                        }
                    }

                    int medianR = GetMedian(redValues);
                    int medianG = GetMedian(greenValues);
                    int medianB = GetMedian(blueValues);

                    result.SetPixel(x, y, Color.FromArgb(medianR, medianG, medianB));
                }
            }

            ProcessImageEdges(original, result, filterSize);

            return result;
        }

        private void ProcessImageEdges(Bitmap original, Bitmap result, int filterSize)
        {
            int width = original.Width;
            int height = original.Height;
            int offset = filterSize / 2;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < offset; y++)
                {
                    int[] redValues = new int[(offset + y + 1) * (2 * offset + 1)];
                    int[] greenValues = new int[(offset + y + 1) * (2 * offset + 1)];
                    int[] blueValues = new int[(offset + y + 1) * (2 * offset + 1)];
                    int index = 0;

                    for (int i = -Math.Min(x, offset); i <= Math.Min(width - 1 - x, offset); i++)
                    {
                        for (int j = 0; j <= offset + y; j++)
                        {
                            if (x + i >= 0 && x + i < width && j >= 0 && j < height)
                            {
                                Color pixel = original.GetPixel(x + i, j);
                                redValues[index] = pixel.R;
                                greenValues[index] = pixel.G;
                                blueValues[index] = pixel.B;
                                index++;
                            }
                        }
                    }

                    Array.Resize(ref redValues, index);
                    Array.Resize(ref greenValues, index);
                    Array.Resize(ref blueValues, index);

                    if (index > 0)
                    {
                        int medianR = GetMedian(redValues);
                        int medianG = GetMedian(greenValues);
                        int medianB = GetMedian(blueValues);

                        result.SetPixel(x, y, Color.FromArgb(medianR, medianG, medianB));

                        result.SetPixel(x, height - 1 - y, Color.FromArgb(medianR, medianG, medianB));
                    }
                }
            }

            // Sol ve sağ kenarlar
            for (int y = offset; y < height - offset; y++)
            {
                for (int x = 0; x < offset; x++)
                {
                    // Sol kenar için değerleri topla
                    int[] redValues = new int[(offset + x + 1) * (2 * offset + 1)];
                    int[] greenValues = new int[(offset + x + 1) * (2 * offset + 1)];
                    int[] blueValues = new int[(offset + x + 1) * (2 * offset + 1)];
                    int index = 0;

                    for (int i = 0; i <= offset + x; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            if (i >= 0 && i < width && y + j >= 0 && y + j < height)
                            {
                                Color pixel = original.GetPixel(i, y + j);
                                redValues[index] = pixel.R;
                                greenValues[index] = pixel.G;
                                blueValues[index] = pixel.B;
                                index++;
                            }
                        }
                    }

                    // Dizileri gerçek boyuta küçült
                    Array.Resize(ref redValues, index);
                    Array.Resize(ref greenValues, index);
                    Array.Resize(ref blueValues, index);

                    if (index > 0)
                    {
                        int medianR = GetMedian(redValues);
                        int medianG = GetMedian(greenValues);
                        int medianB = GetMedian(blueValues);

                        // Sol kenar için uygula
                        result.SetPixel(x, y, Color.FromArgb(medianR, medianG, medianB));

                        // Sağ kenar için uygula
                        result.SetPixel(width - 1 - x, y, Color.FromArgb(medianR, medianG, medianB));
                    }
                }
            }
        }

        private int GetMedian(int[] values)
        {
            int n = values.Length;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (values[j] > values[j + 1])
                    {
                        int temp = values[j];
                        values[j] = values[j + 1];
                        values[j + 1] = temp;
                    }
                }
            }

            if (n % 2 == 0)
            {
                // Çift sayıda eleman varsa ortadaki iki sayının ortalamasını al
                return (values[n / 2 - 1] + values[n / 2]) / 2;
            }
            else
            {
                // Tek sayıda eleman varsa ortadaki sayıyı al
                return values[n / 2];
            }
        }
    }
} 