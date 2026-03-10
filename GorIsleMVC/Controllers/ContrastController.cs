using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GorIsleMVC.Controllers
{
    public class ContrastController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ContrastController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile, double contrast = 1.0)
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

                using var processedImage = AdjustContrast(originalImage, contrast);
                var resultFileName = $"contrast_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await processedImage.SaveAsPngAsync(resultPath);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Kontrast Ayarlama";
                TempData["Parameters"] = $"Kontrast Değeri: {contrast:F2}";
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> AdjustContrast(Image<Rgba32> original, double contrast)
        {
            int width = original.Width, height = original.Height;
            double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
            int[] lut = new int[256];
            for (int i = 0; i < 256; i++)
                lut[i] = (int)Math.Clamp(factor * (i - 128) + 128, 0, 255);

            var result = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = original[x, y];
                    result[x, y] = new Rgba32((byte)lut[p.R], (byte)lut[p.G], (byte)lut[p.B], p.A);
                }
            }
            return result;
        }
    }
}
