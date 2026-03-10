using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                using var processedImage = ApplyUnsharpMask(originalImage, amount, threshold);
                var resultFileName = $"unsharp_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await processedImage.SaveAsPngAsync(resultPath);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Unsharp Masking";
                TempData["Parameters"] = $"Amount: {amount}, Threshold: {threshold}";
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyUnsharpMask(Image<Rgba32> original, float amount, float threshold)
        {
            int width = original.Width, height = original.Height;
            float[,] kernel = { { 1/16f, 2/16f, 1/16f }, { 2/16f, 4/16f, 2/16f }, { 1/16f, 2/16f, 1/16f } };

            var blurred = new Image<Rgba32>(width, height);
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float rSum = 0, gSum = 0, bSum = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            var p = original[x + kx, y + ky];
                            float w = kernel[ky + 1, kx + 1];
                            rSum += p.R * w; gSum += p.G * w; bSum += p.B * w;
                        }
                    }
                    blurred[x, y] = new Rgba32((byte)Math.Clamp((int)rSum, 0, 255), (byte)Math.Clamp((int)gSum, 0, 255), (byte)Math.Clamp((int)bSum, 0, 255), original[x, y].A);
                }
            }

            var result = new Image<Rgba32>(width, height);
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var op = original[x, y];
                    var bp = blurred[x, y];
                    int diffR = op.R - bp.R, diffG = op.G - bp.G, diffB = op.B - bp.B;
                    if (Math.Abs(diffR) > threshold || Math.Abs(diffG) > threshold || Math.Abs(diffB) > threshold)
                    {
                        int r = Math.Clamp(op.R + (int)(diffR * amount), 0, 255);
                        int g = Math.Clamp(op.G + (int)(diffG * amount), 0, 255);
                        int b = Math.Clamp(op.B + (int)(diffB * amount), 0, 255);
                        result[x, y] = new Rgba32((byte)r, (byte)g, (byte)b, op.A);
                    }
                    else
                        result[x, y] = op;
                }
            }
            for (int x = 0; x < width; x++) { result[x, 0] = original[x, 0]; result[x, height - 1] = original[x, height - 1]; }
            for (int y = 0; y < height; y++) { result[0, y] = original[0, y]; result[width - 1, y] = original[width - 1, y]; }
            blurred.Dispose();
            return result;
        }
    }
}
