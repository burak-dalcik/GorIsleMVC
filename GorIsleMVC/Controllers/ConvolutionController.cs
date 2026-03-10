using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                using var resultBitmap = ApplyMean(originalImage, kernelSize);
                var resultFileName = $"mean_filter_{kernelSize}x{kernelSize}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await resultBitmap.SaveAsPngAsync(resultPath);

                TempData["OriginalImage"] = "/uploads/" + originalFileName;
                TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                TempData["KernelSize"] = kernelSize;
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyMean(Image<Rgba32> source, int kernelSize)
        {
            int width = source.Width, height = source.Height;
            var result = new Image<Rgba32>(width, height);
            int padding = kernelSize / 2;

            for (int x = padding; x < width - padding; x++)
            {
                for (int y = padding; y < height - padding; y++)
                {
                    long sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;
                    for (int i = -padding; i <= padding; i++)
                    {
                        for (int j = -padding; j <= padding; j++)
                        {
                            var p = source[x + i, y + j];
                            sumR += p.R; sumG += p.G; sumB += p.B;
                            count++;
                        }
                    }
                    result[x, y] = new Rgba32((byte)(sumR / count), (byte)(sumG / count), (byte)(sumB / count), source[x, y].A);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < padding; y++)
                {
                    result[x, y] = source[x, y];
                    result[x, height - 1 - y] = source[x, height - 1 - y];
                }
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < padding; x++)
                {
                    result[x, y] = source[x, y];
                    result[width - 1 - x, y] = source[width - 1 - x, y];
                }
            }
            return result;
        }
    }
}
