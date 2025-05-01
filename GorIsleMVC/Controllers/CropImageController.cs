using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

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
            var uniqueFileName = $"cropped_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);

            var tempPath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                using (var originalImage = Image.FromFile(tempPath))
                {
                    // Orijinal görüntüyü kaydet
                    originalImage.Save(originalPath, ImageFormat.Jpeg);

                    // Kırpma boyutlarını orijinal görüntü boyutlarına göre ayarla
                    width = Math.Min(width, originalImage.Width - x);
                    height = Math.Min(height, originalImage.Height - y);
                    x = Math.Max(0, Math.Min(x, originalImage.Width - width));
                    y = Math.Max(0, Math.Min(y, originalImage.Height - height));

                    // Kırpılmış görüntü oluştur
                    using (var croppedImage = new Bitmap(width, height))
                    {
                        using (Graphics g = Graphics.FromImage(croppedImage))
                        {
                            var srcRect = new Rectangle(x, y, width, height);
                            var destRect = new Rectangle(0, 0, width, height);
                            g.DrawImage(originalImage, destRect, srcRect, GraphicsUnit.Pixel);
                        }

                        croppedImage.Save(filePath, ImageFormat.Jpeg);
                    }
                }

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
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
} 