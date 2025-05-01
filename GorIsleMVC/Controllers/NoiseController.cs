using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

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
        public async Task<IActionResult> AddNoise(IFormFile imageFile, double noiseDensity)
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
                            var noisyImage = ApplySaltAndPepperNoise(bitmap, noiseDensity);

                            var resultFileName = $"noisy_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            noisyImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["Density"] = noiseDensity;
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
                    var neighbors = new List<int>();

                    // Komşu pikselleri topla
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            Color pixel = original.GetPixel(x + i, y + j);
                            int gray = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                            neighbors.Add(gray);
                        }
                    }

                    // Medyan değeri hesapla
                    neighbors.Sort();
                    int median = neighbors[neighbors.Count / 2];
                    result.SetPixel(x, y, Color.FromArgb(median, median, median));
                }
            }

            // Kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < offset; y++)
                {
                    result.SetPixel(x, y, original.GetPixel(x, y));
                    result.SetPixel(x, height - 1 - y, original.GetPixel(x, height - 1 - y));
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < offset; x++)
                {
                    result.SetPixel(x, y, original.GetPixel(x, y));
                    result.SetPixel(width - 1 - x, y, original.GetPixel(width - 1 - x, y));
                }
            }

            return result;
        }
    }
} 