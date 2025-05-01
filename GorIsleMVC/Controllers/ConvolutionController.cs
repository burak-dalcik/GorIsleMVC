using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ConvolutionController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ConvolutionController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyMeanFilter(IFormFile imageFile, int kernelSize = 3)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction("Index");
            }

            if (kernelSize % 2 == 0 || kernelSize < 3 || kernelSize > 9)
            {
                TempData["Error"] = "Filtre boyutu 3, 5, 7 veya 9 olmalıdır.";
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
                            // Mean filter uygula
                            var resultBitmap = ApplyMean(bitmap, kernelSize);

                            // Sonucu kaydet
                            var resultFileName = $"mean_filter_{kernelSize}x{kernelSize}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            resultBitmap.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = "/uploads/" + originalFileName;
                            TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                            TempData["KernelSize"] = kernelSize;
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

        private Bitmap ApplyMean(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            var resultBitmap = new Bitmap(width, height);

            // Padding miktarı
            int padding = kernelSize / 2;

            // Her piksel için mean filter uygula
            for (int x = padding; x < width - padding; x++)
            {
                for (int y = padding; y < height - padding; y++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    // Kernel içindeki pikselleri topla
                    for (int i = -padding; i <= padding; i++)
                    {
                        for (int j = -padding; j <= padding; j++)
                        {
                            var pixel = sourceBitmap.GetPixel(x + i, y + j);
                            sumR += pixel.R;
                            sumG += pixel.G;
                            sumB += pixel.B;
                            count++;
                        }
                    }

                    // Ortalama değerleri hesapla
                    int avgR = (int)(sumR / count);
                    int avgG = (int)(sumG / count);
                    int avgB = (int)(sumB / count);

                    // Yeni pikseli ayarla
                    resultBitmap.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }

            // Kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < padding; y++)
                {
                    resultBitmap.SetPixel(x, y, sourceBitmap.GetPixel(x, y));
                    resultBitmap.SetPixel(x, height - 1 - y, sourceBitmap.GetPixel(x, height - 1 - y));
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < padding; x++)
                {
                    resultBitmap.SetPixel(x, y, sourceBitmap.GetPixel(x, y));
                    resultBitmap.SetPixel(width - 1 - x, y, sourceBitmap.GetPixel(width - 1 - x, y));
                }
            }

            return resultBitmap;
        }
    }
} 