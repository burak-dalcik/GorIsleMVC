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
                            // Eşikleme uygula
                            var resultBitmap = ApplyThreshold(bitmap, threshold);

                            // Sonucu kaydet
                            var resultFileName = $"threshold_{threshold}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            resultBitmap.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = "/uploads/" + originalFileName;
                            TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                            TempData["Threshold"] = threshold;
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplyThreshold(Bitmap sourceBitmap, int threshold)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            var resultBitmap = new Bitmap(width, height);

            // Her piksel için eşikleme uygula
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = sourceBitmap.GetPixel(x, y);
                    
                    // Gri tonlama değerini hesapla
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    
                    // Eşikleme uygula
                    Color newColor = grayValue >= threshold ? Color.White : Color.Black;
                    
                    resultBitmap.SetPixel(x, y, newColor);
                }
            }

            return resultBitmap;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyOtsuThreshold(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
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
                            // Otsu eşikleme değerini hesapla
                            int otsuThreshold = CalculateOtsuThreshold(bitmap);

                            // Eşikleme uygula
                            var resultBitmap = ApplyThreshold(bitmap, otsuThreshold);

                            // Sonucu kaydet
                            var resultFileName = $"otsu_threshold_{otsuThreshold}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            resultBitmap.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = "/uploads/" + originalFileName;
                            TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                            TempData["Threshold"] = otsuThreshold;
                            TempData["IsOtsu"] = true;
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private int CalculateOtsuThreshold(Bitmap bitmap)
        {
            // Histogram hesapla
            int[] histogram = new int[256];
            int totalPixels = bitmap.Width * bitmap.Height;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    histogram[grayValue]++;
                }
            }

            // Otsu metodu ile optimal eşik değerini hesapla
            double sum = 0;
            for (int i = 0; i < 256; i++)
                sum += i * histogram[i];

            double sumB = 0;
            int wB = 0;
            int wF = 0;

            double maxVariance = 0;
            int threshold = 0;

            for (int t = 0; t < 256; t++)
            {
                wB += histogram[t];
                if (wB == 0) continue;

                wF = totalPixels - wB;
                if (wF == 0) break;

                sumB += t * histogram[t];

                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;

                double variance = wB * wF * Math.Pow(mB - mF, 2);

                if (variance > maxVariance)
                {
                    maxVariance = variance;
                    threshold = t;
                }
            }

            return threshold;
        }
    }
} 