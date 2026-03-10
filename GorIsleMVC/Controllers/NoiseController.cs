using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GorIsleMVC.Controllers
{
    public class NoiseController : Controller
    {
        private readonly Random _random = new();
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

            double densityValue = Math.Clamp(noiseDensity, 0, 100) / 100.0;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                using var noisyImage = ApplySaltAndPepperNoise(originalImage, densityValue);
                var resultFileName = $"noisy_{noiseDensity}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await noisyImage.SaveAsPngAsync(resultPath);

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

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                using var filteredImage = ApplyMedianFilter(originalImage, filterSize);
                var resultFileName = $"filtered_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await filteredImage.SaveAsPngAsync(resultPath);

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
        }

        private Image<Rgba32> ApplySaltAndPepperNoise(Image<Rgba32> original, double density)
        {
            int width = original.Width, height = original.Height;
            var result = original.Clone();
            int totalPixels = width * height;
            int noisePixels = (int)(totalPixels * density);
            for (int i = 0; i < noisePixels; i++)
            {
                int x = _random.Next(width);
                int y = _random.Next(height);
                byte v = (byte)(_random.NextDouble() < 0.5 ? 0 : 255);
                result[x, y] = new Rgba32(v, v, v, original[x, y].A);
            }
            return result;
        }

        private Image<Rgba32> ApplyMedianFilter(Image<Rgba32> original, int filterSize)
        {
            int width = original.Width, height = original.Height;
            var result = original.Clone();
            int offset = filterSize / 2;
            int size = filterSize * filterSize;
            int[] r = new int[size], g = new int[size], b = new int[size];

            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    int idx = 0;
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            var p = original[x + i, y + j];
                            r[idx] = p.R; g[idx] = p.G; b[idx] = p.B;
                            idx++;
                        }
                    }
                    Array.Sort(r, 0, idx);
                    Array.Sort(g, 0, idx);
                    Array.Sort(b, 0, idx);
                    int m = idx / 2;
                    result[x, y] = new Rgba32((byte)r[m], (byte)g[m], (byte)b[m], original[x, y].A);
                }
            }
            return result;
        }
    }
}
