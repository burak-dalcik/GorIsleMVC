using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class PrewittController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public PrewittController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile)
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
                            var processedImage = ApplyPrewittOperator(bitmap);

                            var resultFileName = $"prewitt_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            processedImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["ProcessType"] = "Prewitt Kenar Bulma";
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

        private Bitmap ApplyPrewittOperator(Bitmap original)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);


            int[,] prewittX = new int[,] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
            int[,] prewittY = new int[,] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int gx = 0;
                    int gy = 0;


                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            Color pixel = original.GetPixel(x + j, y + i);
                            int gray = (int)((pixel.R + pixel.G + pixel.B) / 3.0);

                            gx += gray * prewittX[i + 1, j + 1];
                            gy += gray * prewittY[i + 1, j + 1];
                        }
                    }

                    // Gradyan büyüklüğü hesaplama
                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                    magnitude = Math.Min(255, Math.Max(0, magnitude));

                    result.SetPixel(x, y, Color.FromArgb(magnitude, magnitude, magnitude));
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

            return result;
        }
    }
} 