using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class UnsharpController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public UnsharpController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile, float amount = 1.5f, float threshold = 0)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var originalImage = Image.FromStream(stream))
                    {
                        originalImage.Save(originalPath, ImageFormat.Png);

                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var processedImage = ApplyUnsharpMask(bitmap, amount, threshold);

                            var resultFileName = $"unsharp_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            processedImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["ProcessType"] = "Unsharp Masking";
                            TempData["Parameters"] = $"Amount: {amount}, Threshold: {threshold}";
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplyUnsharpMask(Bitmap original, float amount, float threshold)
        {
            int width = original.Width;
            int height = original.Height;
            
            // Gaussian Blur için kernel
            float[,] kernel = {
                { 1/16f, 2/16f, 1/16f },
                { 2/16f, 4/16f, 2/16f },
                { 1/16f, 2/16f, 1/16f }
            };

            // Blur uygulanmış kopya oluştur
            Bitmap blurred = new Bitmap(width, height);
            
            // Gaussian Blur uygula
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float rSum = 0, gSum = 0, bSum = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixel = original.GetPixel(x + kx, y + ky);
                            float weight = kernel[ky + 1, kx + 1];

                            rSum += pixel.R * weight;
                            gSum += pixel.G * weight;
                            bSum += pixel.B * weight;
                        }
                    }

                    int r = Math.Min(255, Math.Max(0, (int)rSum));
                    int g = Math.Min(255, Math.Max(0, (int)gSum));
                    int b = Math.Min(255, Math.Max(0, (int)bSum));

                    blurred.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            Bitmap result = new Bitmap(width, height);

            // Unsharp Mask uygula
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    Color originalPixel = original.GetPixel(x, y);
                    Color blurredPixel = blurred.GetPixel(x, y);

                    
                    int diffR = originalPixel.R - blurredPixel.R;
                    int diffG = originalPixel.G - blurredPixel.G;
                    int diffB = originalPixel.B - blurredPixel.B;

                    // Threshold kontrolü
                    if (Math.Abs(diffR) > threshold || Math.Abs(diffG) > threshold || Math.Abs(diffB) > threshold)
                    {
                        // Keskinleştirme uygula
                        int newR = Math.Min(255, Math.Max(0, originalPixel.R + (int)(diffR * amount)));
                        int newG = Math.Min(255, Math.Max(0, originalPixel.G + (int)(diffG * amount)));
                        int newB = Math.Min(255, Math.Max(0, originalPixel.B + (int)(diffB * amount)));

                        result.SetPixel(x, y, Color.FromArgb(newR, newG, newB));
                    }
                    else
                    {
                        // Threshold altındaki pikselleri kopyala
                        result.SetPixel(x, y, originalPixel);
                    }
                }
            }

            // Kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                result.SetPixel(x, 0, original.GetPixel(x, 0));
                result.SetPixel(x, height - 1, original.GetPixel(x, height - 1));
            }

            for (int y = 0; y < height; y++)
            {
                result.SetPixel(0, y, original.GetPixel(0, y));
                result.SetPixel(width - 1, y, original.GetPixel(width - 1, y));
            }

            blurred.Dispose();
            return result;
        }
    }
} 