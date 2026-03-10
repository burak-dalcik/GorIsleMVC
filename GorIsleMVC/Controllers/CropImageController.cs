using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GorIsleMVC.Controllers
{
    public class CropImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public CropImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, int x = 0, int y = 0, int width = 100, int height = 100)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction(nameof(Upload));
            }

            if (!imageFile.ContentType.StartsWith("image/"))
            {
                TempData["Error"] = "Lütfen geçerli bir görsel dosyası yükleyin.";
                return RedirectToAction(nameof(Upload));
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"cropped_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);

            try
            {
                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsJpegAsync(originalPath);

                width = Math.Min(width, originalImage.Width - x);
                height = Math.Min(height, originalImage.Height - y);
                x = Math.Max(0, Math.Min(x, originalImage.Width - width));
                y = Math.Max(0, Math.Min(y, originalImage.Height - height));

                var cropRect = new SixLabors.ImageSharp.Rectangle(x, y, width, height);
                using var croppedImage = originalImage.Clone(ctx => ctx.Crop(cropRect));
                await croppedImage.SaveAsJpegAsync(filePath);

                TempData["ProcessedImage"] = uniqueFileName;
                TempData["OriginalImage"] = originalFileName;
                TempData["CropX"] = x;
                TempData["CropY"] = y;
                TempData["CropWidth"] = width;
                TempData["CropHeight"] = height;
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }
    }
}
