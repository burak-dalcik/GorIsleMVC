using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ColorSpaceController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ColorSpaceController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, string colorSpace = "HSV")
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
            var uniqueFileName = $"colorspace_{DateTime.Now:yyyyMMddHHmmss}.jpg";
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

                    // Renk uzayı dönüşümü için yeni görüntü oluştur
                    using (var convertedImage = new Bitmap(originalImage.Width, originalImage.Height))
                    {
                        for (int x = 0; x < originalImage.Width; x++)
                        {
                            for (int y = 0; y < originalImage.Height; y++)
                            {
                                var pixel = ((Bitmap)originalImage).GetPixel(x, y);
                                Color newColor;

                                switch (colorSpace.ToUpper())
                                {
                                    case "HSV":
                                        newColor = RGBtoHSV(pixel);
                                        break;
                                    case "YUV":
                                        newColor = RGBtoYUV(pixel);
                                        break;
                                    default:
                                        newColor = pixel;
                                        break;
                                }

                                convertedImage.SetPixel(x, y, newColor);
                            }
                        }

                        convertedImage.Save(filePath, ImageFormat.Jpeg);
                    }
                }

                TempData["ProcessedImage"] = uniqueFileName;
                TempData["OriginalImage"] = originalFileName;
                TempData["ColorSpace"] = colorSpace;
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

        private Color RGBtoHSV(Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            // Hue calculation
            double h = 0;
            if (delta != 0)
            {
                if (max == r)
                    h = 60 * (((g - b) / delta) % 6);
                else if (max == g)
                    h = 60 * (((b - r) / delta) + 2);
                else
                    h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            // Saturation calculation
            double s = max == 0 ? 0 : delta / max;

            // Value calculation
            double v = max;

            // Convert back to 0-255 range
            return Color.FromArgb(
                rgb.A,
                (byte)(h / 360 * 255),
                (byte)(s * 255),
                (byte)(v * 255)
            );
        }

        private Color RGBtoYUV(Color rgb)
        {
            // RGB to YUV conversion
            byte y = (byte)(0.299 * rgb.R + 0.587 * rgb.G + 0.114 * rgb.B);
            byte u = (byte)(128 - 0.168736 * rgb.R - 0.331264 * rgb.G + 0.5 * rgb.B);
            byte v = (byte)(128 + 0.5 * rgb.R - 0.418688 * rgb.G - 0.081312 * rgb.B);

            return Color.FromArgb(rgb.A, y, u, v);
        }
    }
} 