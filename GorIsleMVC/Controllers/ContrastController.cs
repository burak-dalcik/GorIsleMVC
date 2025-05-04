using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ContrastController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ContrastController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile, double contrast = 1.0)
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
                            var processedImage = AdjustContrast(bitmap, contrast);

                            var resultFileName = $"contrast_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            processedImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["ProcessType"] = "Kontrast Ayarlama";
                            TempData["Parameters"] = $"Kontrast Değeri: {contrast:F2}";
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

        private Bitmap AdjustContrast(Bitmap original, double contrast)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);

            double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

            int[] lookupTable = new int[256];
            for (int i = 0; i < 256; i++)
            {
                double temp = factor * (i - 128) + 128;
                lookupTable[i] = (int)Math.Min(255, Math.Max(0, temp));
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = original.GetPixel(x, y);
                    
                    int r = lookupTable[pixel.R];
                    int g = lookupTable[pixel.G];
                    int b = lookupTable[pixel.B];

                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }
    }
} 