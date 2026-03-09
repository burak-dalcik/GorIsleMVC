using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using GorIsleMVC.Helpers;

namespace GorIsleMVC.Controllers
{
    public class BackgroundRemovalController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public BackgroundRemovalController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            if (!imageFile.ContentType.StartsWith("image/"))
            {
                TempData["Error"] = "Lütfen geçerli bir görüntü dosyası yükleyin.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Arka plan rengini belirlerken kullanılacak tolerans (renk farkı)
                // Kullanıcıdan gizli, sabit ve nispeten dar tutuluyor.
                byte threshold = 40;

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var originalFileName = $"original_{timestamp}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                var resultFileName = $"background_{timestamp}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var originalImage = Image.FromStream(stream))
                    {
                        originalImage.Save(originalPath, ImageFormat.Png);

                        using (var bitmap = new Bitmap(originalImage))
                        using (var processedImage = BackgroundRemovalHelper.RemoveBackground(bitmap, threshold))
                        {
                            processedImage.Save(resultPath, ImageFormat.Png);
                        }
                    }
                }

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Arka Plan Çıkarma";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
