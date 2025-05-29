using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class RotateImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public RotateImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, float angle = 90)
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
                resultBitmap = ApplyRotation(bitmap, angle);

                var resultFileName = $"rotated_{angle}deg_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                resultBitmap.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = "/uploads/" + originalFileName;
                TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                TempData["Angle"] = angle;

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
                bitmap?.Dispose();
                resultBitmap?.Dispose();
            }
        }

        private Bitmap ApplyRotation(Bitmap sourceBitmap, float angleDegrees)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // Açıyı radyana çevir
            double angleRadians = angleDegrees * Math.PI / 180.0;
            double cosTheta = Math.Cos(angleRadians);
            double sinTheta = Math.Sin(angleRadians);

            int newWidth = (int)(Math.Abs(width * cosTheta) + Math.Abs(height * sinTheta)) + 1;
            int newHeight = (int)(Math.Abs(width * sinTheta) + Math.Abs(height * cosTheta)) + 1;

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];      // ARGB formatında orijinal
            byte[,,,] resultPixels = new byte[newWidth, newHeight, 4, 1]; // Döndürülmüş sonuç

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

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    resultPixels[x, y, 0, 0] = 255; // Alpha (opak)
                    resultPixels[x, y, 1, 0] = 255; // Red (beyaz)
                    resultPixels[x, y, 2, 0] = 255; // Green (beyaz)
                    resultPixels[x, y, 3, 0] = 255; // Blue (beyaz)
                }
            }
            double centerX = width / 2.0;
            double centerY = height / 2.0;
            double newCenterX = newWidth / 2.0;
            double newCenterY = newHeight / 2.0;

            for (int newX = 0; newX < newWidth; newX++)
            {
                for (int newY = 0; newY < newHeight; newY++)
                {
                    double relativeNewX = newX - newCenterX;
                    double relativeNewY = newY - newCenterY;

                    double originalRelativeX = relativeNewX * cosTheta + relativeNewY * sinTheta;
                    double originalRelativeY = -relativeNewX * sinTheta + relativeNewY * cosTheta;

                    int originalX = (int)Math.Round(originalRelativeX + centerX);
                    int originalY = (int)Math.Round(originalRelativeY + centerY);

                    if (originalX >= 0 && originalX < width && originalY >= 0 && originalY < height)
                    {
                        resultPixels[newX, newY, 0, 0] = sourcePixels[originalX, originalY, 0, 0]; // Alpha
                        resultPixels[newX, newY, 1, 0] = sourcePixels[originalX, originalY, 1, 0]; // Red
                        resultPixels[newX, newY, 2, 0] = sourcePixels[originalX, originalY, 2, 0]; // Green
                        resultPixels[newX, newY, 3, 0] = sourcePixels[originalX, originalY, 3, 0]; // Blue
                    }
                }
            }

            Bitmap resultBitmap = new Bitmap(newWidth, newHeight);
            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
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