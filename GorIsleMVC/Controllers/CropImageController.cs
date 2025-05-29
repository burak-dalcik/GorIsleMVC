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
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"cropped_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap originalBitmap = null;
            Bitmap croppedBitmap = null;

            try
            {
                var uploadsFolder2 = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder2))
                    Directory.CreateDirectory(uploadsFolder2);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Jpeg);

                // Kırpma boyutlarını orijinal görüntü boyutlarına göre ayarla
                width = Math.Min(width, originalImage.Width - x);
                height = Math.Min(height, originalImage.Height - y);
                x = Math.Max(0, Math.Min(x, originalImage.Width - width));
                y = Math.Max(0, Math.Min(y, originalImage.Height - y));

                originalBitmap = new Bitmap(originalImage);
                croppedBitmap = CropImageManual(originalBitmap, x, y, width, height);

                croppedBitmap.Save(filePath, ImageFormat.Jpeg);

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
                stream?.Dispose();
                originalImage?.Dispose();
                originalBitmap?.Dispose();
                croppedBitmap?.Dispose();
            }
        }

        private Bitmap CropImageManual(Bitmap sourceBitmap, int cropX, int cropY, int cropWidth, int cropHeight)
        {
            int sourceWidth = sourceBitmap.Width;
            int sourceHeight = sourceBitmap.Height;

            cropX = Math.Max(0, Math.Min(cropX, sourceWidth - 1));
            cropY = Math.Max(0, Math.Min(cropY, sourceHeight - 1));
            cropWidth = Math.Max(1, Math.Min(cropWidth, sourceWidth - cropX));
            cropHeight = Math.Max(1, Math.Min(cropHeight, sourceHeight - cropY));

            byte[,,,] sourcePixels = new byte[sourceWidth, sourceHeight, 4, 1];  // ARGB formatında orijinal
            byte[,,,] croppedPixels = new byte[cropWidth, cropHeight, 4, 1];    // Kırpılmış sonuç

            for (int x = 0; x < sourceWidth; x++)
            {
                for (int y = 0; y < sourceHeight; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1, 0] = pixel.R; // Red
                    sourcePixels[x, y, 2, 0] = pixel.G; // Green
                    sourcePixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            for (int x = 0; x < cropWidth; x++)
            {
                for (int y = 0; y < cropHeight; y++)
                {
                    int sourceX = cropX + x;
                    int sourceY = cropY + y;

                    if (sourceX < sourceWidth && sourceY < sourceHeight)
                    {
                        croppedPixels[x, y, 0, 0] = sourcePixels[sourceX, sourceY, 0, 0]; // Alpha
                        croppedPixels[x, y, 1, 0] = sourcePixels[sourceX, sourceY, 1, 0]; // Red
                        croppedPixels[x, y, 2, 0] = sourcePixels[sourceX, sourceY, 2, 0]; // Green
                        croppedPixels[x, y, 3, 0] = sourcePixels[sourceX, sourceY, 3, 0]; // Blue
                    }
                    else
                    {
                        croppedPixels[x, y, 0, 0] = 255; // Alpha (opak)
                        croppedPixels[x, y, 1, 0] = 0;   // Red
                        croppedPixels[x, y, 2, 0] = 0;   // Green
                        croppedPixels[x, y, 3, 0] = 0;   // Blue
                    }
                }
            }

            Bitmap resultBitmap = new Bitmap(cropWidth, cropHeight);
            for (int x = 0; x < cropWidth; x++)
            {
                for (int y = 0; y < cropHeight; y++)
                {
                    byte alpha = croppedPixels[x, y, 0, 0];
                    byte red = croppedPixels[x, y, 1, 0];
                    byte green = croppedPixels[x, y, 2, 0];
                    byte blue = croppedPixels[x, y, 3, 0];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }
    }
}