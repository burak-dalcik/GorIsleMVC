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
            Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur

            var uniqueFileName = $"binary_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);
            var tempPath = Path.GetTempFileName();

            try
            {
                // Yüklenen dosyayı geçici konuma kaydet
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                using (var originalImage = new Bitmap(tempPath))
                {
                    // Orijinal görselı kaydet
                    originalImage.Save(originalPath, ImageFormat.Jpeg);

                    int width = originalImage.Width;
                    int height = originalImage.Height;

                    // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
                    byte[,,,] originalPixels = new byte[width, height, 4, 1]; // ARGB formatında
                    byte[,,,] binaryPixels = new byte[width, height, 4, 1];   // Binary sonuç

                    // ADIM 1: Orijinal görselin piksellerini array'e aktar
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Color pixel = originalImage.GetPixel(x, y);
                            originalPixels[x, y, 0, 0] = pixel.A; // Alpha
                            originalPixels[x, y, 1, 0] = pixel.R; // Red
                            originalPixels[x, y, 2, 0] = pixel.G; // Green
                            originalPixels[x, y, 3, 0] = pixel.B; // Blue
                        }
                    }

                    // ADIM 2: Array üzerinde binary dönüşüm işlemi yap
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            // RGB değerlerini array'den al
                            byte alpha = originalPixels[x, y, 0, 0];
                            byte red = originalPixels[x, y, 1, 0];
                            byte green = originalPixels[x, y, 2, 0];
                            byte blue = originalPixels[x, y, 3, 0];

                            // Gri tonlama hesaplama (kendi algoritman)
                            byte grayValue = (byte)((red + green + blue) / 3);

                            // Binary dönüşüm (threshold ile karşılaştırma)
                            byte binaryValue = (byte)(grayValue > threshold ? 255 : 0);

                            // Binary sonucu array'e kaydet
                            binaryPixels[x, y, 0, 0] = alpha;       // Alpha değeri aynı kalır
                            binaryPixels[x, y, 1, 0] = binaryValue; // Red = binary değer
                            binaryPixels[x, y, 2, 0] = binaryValue; // Green = binary değer
                            binaryPixels[x, y, 3, 0] = binaryValue; // Blue = binary değer
                        }
                    }

                    // ADIM 3: Binary array'den yeni Bitmap oluştur ve kaydet
                    using (var binaryImage = new Bitmap(width, height))
                    {
                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                byte alpha = binaryPixels[x, y, 0, 0];
                                byte red = binaryPixels[x, y, 1, 0];
                                byte green = binaryPixels[x, y, 2, 0];
                                byte blue = binaryPixels[x, y, 3, 0];

                                Color binaryColor = Color.FromArgb(alpha, red, green, blue);
                                binaryImage.SetPixel(x, y, binaryColor);
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