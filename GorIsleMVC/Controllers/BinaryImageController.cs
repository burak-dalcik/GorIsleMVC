using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class BinaryImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public BinaryImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, int threshold = 128)
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
            var uniqueFileName = $"binary_{DateTime.Now:yyyyMMddHHmmss}.jpg";
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

                    // İkili (binary) görüntü oluştur
                    using (var binaryImage = new Bitmap(originalImage.Width, originalImage.Height))
                    {
                        for (int x = 0; x < originalImage.Width; x++)
                        {
                            for (int y = 0; y < originalImage.Height; y++)
                            {
                                var pixel = ((Bitmap)originalImage).GetPixel(x, y);
                                // Önce griye çevir
                                var grayValue = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                                // Eşik değerine göre siyah veya beyaz yap
                                var binaryValue = (byte)(grayValue > threshold ? 255 : 0);
                                binaryImage.SetPixel(x, y, Color.FromArgb(pixel.A, binaryValue, binaryValue, binaryValue));
                            }
                        }

                        binaryImage.Save(filePath, ImageFormat.Jpeg);
                    }
                }

                TempData["ProcessedImage"] = uniqueFileName;
                TempData["OriginalImage"] = originalFileName;
                TempData["Threshold"] = threshold;
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