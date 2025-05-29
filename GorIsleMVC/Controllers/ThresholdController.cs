using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

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

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap resultBitmap = null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Png);

                bitmap = new Bitmap(originalImage);
                resultBitmap = ApplyThreshold(bitmap, threshold);

                var resultFileName = $"threshold_{threshold}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                resultBitmap.Save(resultPath, ImageFormat.Png);

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
            finally
            {
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                resultBitmap?.Dispose();
            }
        }

        private Bitmap ApplyThreshold(Bitmap sourceBitmap, int threshold)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Threshold sonucu

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

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte red = sourcePixels[x, y, 1, 0];
                    byte green = sourcePixels[x, y, 2, 0];
                    byte blue = sourcePixels[x, y, 3, 0];

                    int grayValue = (red + green + blue) / 3;

                    byte thresholdedValue = (byte)(grayValue >= threshold ? 255 : 0);

                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha değeri aynı kalır
                    resultPixels[x, y, 1, 0] = thresholdedValue; // Red - Beyaz veya Siyah
                    resultPixels[x, y, 2, 0] = thresholdedValue; // Green - Beyaz veya Siyah
                    resultPixels[x, y, 3, 0] = thresholdedValue; // Blue - Beyaz veya Siyah
                }
            }

            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = resultPixels[x, y, 0, 0];
                    byte red = resultPixels[x, y, 1, 0];
                    byte green = resultPixels[x, y, 2, 0];
                    byte blue = resultPixels[x, y, 3, 0];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }
    }
}