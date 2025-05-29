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

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] grayPixels = new byte[width, height, 4, 1];    // Grayscale sonucu

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
                    byte alpha = sourcePixels[x, y, 0, 0];


                    byte grayValue = (byte)((red + green + blue) / 3);

     
                    grayPixels[x, y, 0, 0] = alpha;     // Alpha aynı kalır
                    grayPixels[x, y, 1, 0] = grayValue; // Red = Gray
                    grayPixels[x, y, 2, 0] = grayValue; // Green = Gray
                    grayPixels[x, y, 3, 0] = grayValue; // Blue = Gray
                }
            }

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