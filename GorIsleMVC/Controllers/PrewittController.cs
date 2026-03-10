using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                using var processedImage = ApplyPrewittOperator(originalImage);
                var resultFileName = $"prewitt_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await processedImage.SaveAsPngAsync(resultPath);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Prewitt Kenar Bulma";
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyPrewittOperator(Image<Rgba32> original)
        {
            int width = original.Width, height = original.Height;
            var result = new Image<Rgba32>(width, height);
            int[,] prewittX = { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
            int[,] prewittY = { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int gx = 0, gy = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            var p = original[x + j, y + i];
                            int gray = (p.R + p.G + p.B) / 3;
                            gx += gray * prewittX[i + 1, j + 1];
                            gy += gray * prewittY[i + 1, j + 1];
                        }
                    }
                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                    magnitude = Math.Clamp(magnitude, 0, 255);
                    result[x, y] = new Rgba32((byte)magnitude, (byte)magnitude, (byte)magnitude, original[x, y].A);
                }
            }

            for (int x = 0; x < width; x++)
            {
                result[x, 0] = original[x, 0];
                result[x, height - 1] = original[x, height - 1];
            }
            for (int y = 0; y < height; y++)
            {
                result[0, y] = original[0, y];
                result[width - 1, y] = original[width - 1, y];
            }
            return result;
        }
    }
}
