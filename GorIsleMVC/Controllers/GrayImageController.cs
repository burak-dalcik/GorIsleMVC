using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class GrayImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public GrayImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile)
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

            var uniqueFileName = $"gray_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap originalBitmap = null;
            Bitmap grayBitmap = null;

            try
            {
                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Jpeg);

                originalBitmap = new Bitmap(originalImage);
                grayBitmap = ConvertToGrayscaleManual(originalBitmap);

                grayBitmap.Save(filePath, ImageFormat.Jpeg);

                TempData["ProcessedImage"] = uniqueFileName;
                TempData["OriginalImage"] = originalFileName;

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
            finally
            {
                // Manuel cleanup işlemleri
                stream?.Dispose();
                originalImage?.Dispose();
                originalBitmap?.Dispose();
                grayBitmap?.Dispose();
            }
        }

        private Bitmap ConvertToGrayscaleManual(Bitmap sourceBitmap)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] grayPixels = new byte[width, height, 4, 1];    // Grayscale sonucu

            // ADIM 1: Orijinal görselin piksellerini array'e aktar
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1, 0] = pixel.R; // Red
                    sourcePixels[x, y, 2, 0] = pixel.G; // Green
                    sourcePixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            // ADIM 2: Array üzerinde grayscale dönüşümü yap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Array'den RGB değerlerini al
                    byte red = sourcePixels[x, y, 1, 0];
                    byte green = sourcePixels[x, y, 2, 0];
                    byte blue = sourcePixels[x, y, 3, 0];
                    byte alpha = sourcePixels[x, y, 0, 0];

                    // 3 farklı grayscale yöntemi seçeneği:

                    // Yöntem 1: Basit ortalama (orijinal kodda kullanılan)
                    byte grayValue = (byte)((red + green + blue) / 3);

                    // Yöntem 2: Ağırlıklı ortalama (İnsan gözünün algısına daha yakın)
                    // byte grayValue = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);

                    // Yöntem 3: Luminance formülü (ITU-R BT.709 standardı)
                    // byte grayValue = (byte)(0.2126 * red + 0.7152 * green + 0.0722 * blue);

                    // Sonucu array'e kaydet
                    grayPixels[x, y, 0, 0] = alpha;     // Alpha aynı kalır
                    grayPixels[x, y, 1, 0] = grayValue; // Red = Gray
                    grayPixels[x, y, 2, 0] = grayValue; // Green = Gray
                    grayPixels[x, y, 3, 0] = grayValue; // Blue = Gray
                }
            }

            // ADIM 3: Grayscale array'den yeni Bitmap oluştur
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = grayPixels[x, y, 0, 0];
                    byte grayValue = grayPixels[x, y, 1, 0]; // R, G, B hepsi aynı değer

                    Color resultColor = Color.FromArgb(alpha, grayValue, grayValue, grayValue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }
    }
}