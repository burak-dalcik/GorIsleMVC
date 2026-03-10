using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GorIsleMVC.Controllers
{
    public class ThresholdController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ThresholdController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyThreshold(IFormFile imageFile, int threshold = 128)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction("Index");
            }

            if (threshold < 0 || threshold > 255)
            {
                TempData["Error"] = "Eşik değeri 0-255 arasında olmalıdır.";
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

                using var resultBitmap = ApplyThreshold(originalImage, threshold);
                var resultFileName = $"threshold_{threshold}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await resultBitmap.SaveAsPngAsync(resultPath);

                TempData["OriginalImage"] = "/uploads/" + originalFileName;
                TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                TempData["Threshold"] = threshold;
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyThreshold(Image<Rgba32> source, int threshold)
        {
            int width = source.Width, height = source.Height;
            var result = new Image<Rgba32>(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = source[x, y];
                    int gray = (p.R + p.G + p.B) / 3;
                    byte v = (byte)(gray >= threshold ? 255 : 0);
                    result[x, y] = new Rgba32(v, v, v, p.A);
                }
            }
            return result;
        }
    }
}
